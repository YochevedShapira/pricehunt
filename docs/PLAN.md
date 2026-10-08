# PriceHunt – Implementation Plan

Home assignment for Ship4wd. Stack: .NET 10 (LTS) + Angular (latest stable, >= 17).
This file is the working plan; each numbered step is one commit so the git
history tells the story of how the solution was built.

---

## 1. Architecture decisions (and why)

Decisions confirmed on 2026-10-08: SSE for streaming, Clean Architecture in
four projects, NUnit + NSubstitute for tests, EF Core migrations applied at
startup.

Every choice below was made to be *explainable*, not to be the fastest path.
Each one lists the alternatives that were rejected and the reason.

### 1.1 Streaming: Server-Sent Events (SSE)

**Chosen:** `GET /api/searches/stream` returning `text/event-stream`, built on
.NET's native `TypedResults.ServerSentEvents` + `IAsyncEnumerable<SseItem<T>>`.

Why SSE fits this problem exactly:

| Requirement in the brief                      | How SSE satisfies it                                                                 |
|-----------------------------------------------|---------------------------------------------------------------------------------------|
| Stream each result as soon as it arrives      | One long-lived HTTP response; each supplier result is one event flushed immediately. |
| Client cancel / disconnect must stop the work | Closing the connection fires `HttpContext.RequestAborted`; that token is the root of every supplier call. Cancellation *is* the HTTP connection lifetime, no extra protocol. |
| New search cancels the old one                | Client unsubscribes the previous `Observable` (RxJS `switchMap`) which closes the old `EventSource`. |
| "Tell the client the search has ended"        | A typed terminal event (`ended`) carrying the final status.                           |
| Different event kinds (quote, failure, ended) | SSE has named events built in; the client dispatches on the event name.              |

Rejected alternatives:

- **SignalR** – bidirectional, connection/hub management, extra client library.
  The data flow here is strictly server → client after the initial request, so
  SignalR's extra capability is unused weight (YAGNI). It also hides the
  transport, which makes the cancellation story harder to explain.
- **Chunked NDJSON over fetch** – works, but there is no standard framing, no
  named events, and the client needs a hand-written stream parser.
- **WebSockets** – same objection as SignalR, with none of its conveniences.
- **gRPC server streaming** – not browser-native without grpc-web proxying.

Known trade-off (document in README): `EventSource` is GET-only, so search
parameters go in the query string. This is acceptable: the search is
idempotent from the client's point of view and the parameters are small.
`EventSource` also auto-reconnects when the server closes the stream, so the
client must `close()` on the terminal event. A server-issued `searchId` in the
first event is used as a second guard so a late event from an old search can
never be rendered.

### 1.2 Concurrency model: fan-out into a Channel, single consumer

- Each selected supplier runs as its own `Task` writing its outcome into a
  `System.Threading.Channels.Channel<SupplierOutcome>`.
- The orchestrator is the **single reader** of the channel and yields an
  `IAsyncEnumerable<SearchEvent>`. Because there is one consumer, persistence
  (which uses a non-thread-safe `DbContext`) is naturally serialised with no
  locks.
- **Timeout vs cancellation** are distinguished explicitly: a linked
  `CancellationTokenSource` with `CancelAfter(6s)` is created from the request
  token. When the loop exits, `requestToken.IsCancellationRequested` means
  *Cancelled*; otherwise, if the timeout fired, *TimedOut*; otherwise
  *Completed*. The brief requires all three statuses to be persisted.
- A supplier that throws is caught **inside its own task** and converted into a
  `SupplierFailed` outcome. One failure cannot affect the others.
- All timing goes through `TimeProvider` (.NET 8+), so timeout and delay
  logic is unit-tested deterministically with `FakeTimeProvider` – no
  `Thread.Sleep` in tests.

Rejected: a `Task.WhenAny` loop (O(n²), awkward to cancel), Rx.NET (an extra
dependency for something Channels does idiomatically).

### 1.3 Solution structure: Clean Architecture, four projects

```
server/
  src/
    PriceHunt.Domain          entities, value objects, enums – zero dependencies
    PriceHunt.Application     use cases + ports (interfaces) – depends on Domain
    PriceHunt.Infrastructure  EF Core/SQLite, simulated suppliers – depends on Application
    PriceHunt.Api             minimal API endpoints, SSE, DI composition root
  tests/
    PriceHunt.Domain.Tests
    PriceHunt.Application.Tests
    PriceHunt.Infrastructure.Tests
    PriceHunt.Api.Tests       in-process integration tests (WebApplicationFactory)
```

The dependency rule points inward: Domain knows nothing, Application defines
the ports (`ISupplier`, `ISearchRepository`, ...) and Infrastructure implements
them. The interview-ready consequence: the fan-out logic is tested with fake
suppliers and a fake clock, and swapping a simulated supplier for a real HTTP
adapter touches Infrastructure only.

Why not fewer projects: the brief explicitly asks for design reasoning; the
Domain/Application split is what lets the orchestration be tested without EF
Core or ASP.NET in the test process.

### 1.4 Suppliers: one interface, one simulated implementation, seven profiles

- `ISupplier { string Name; Task<Quote> GetQuoteAsync(QuoteRequest, CancellationToken); }`
- `SimulatedSupplier` is a single class configured by a `SupplierProfile`
  (delay range, failure probability, never-responds flag). Seven profiles are
  registered via an `ISupplierCatalog`. Seven copy-pasted classes would
  violate DRY and hide the fact that they differ only in data.
- "Never responds" is implemented as `await Delay(Timeout.Infinite, ct)` so it
  still honours cancellation – the brief says cancellation must *propagate to
  the supplier calls*.
- Failure is a thrown `SupplierUnavailableException`, mirroring what a real
  HTTP adapter would do.
- `GET /api/suppliers` exposes the catalog so the Angular app does not hardcode
  names.

### 1.5 Persistence: EF Core + SQLite + migrations

- SQLite file database, created with `Database.Migrate()` on startup.
  **Migrations, not `EnsureCreated()`**: `EnsureCreated` cannot evolve a schema
  and is a dead end the moment a column is added. Migrations are the production
  path and still give "schema created automatically on first run".
- Persistence is a separate responsibility (`ISearchRecorder` →
  `ISearchRepository`), composed with the fan-out by the use case. The fan-out
  does not know a database exists.
- Final-status writes use `CancellationToken.None`: the request token is
  already cancelled at that point and the whole purpose of the write is to
  record that fact.

Tables:

```
Searches
  Id              GUID  PK
  FromLocation    TEXT
  ToLocation      TEXT
  FromDate        DATE
  ToDate          DATE
  StartedAt       DATETIME (UTC)   indexed
  EndedAt         DATETIME (UTC)   null while running
  Status          INT   enum: Running, Completed, TimedOut, Cancelled

SearchSuppliers                       (the suppliers that were *requested*)
  SearchId        GUID  FK -> Searches
  SupplierName    TEXT
  PK (SearchId, SupplierName)

SupplierResponses                     (one row per supplier per search, success or not)
  Id              INT   PK
  SearchId        GUID  FK -> Searches     indexed
  SupplierName    TEXT                     indexed with RecordedAt
  Outcome         INT   enum: Quoted, Failed, NoResponse
  Price           DECIMAL  null unless Quoted
  Currency        TEXT     null unless Quoted
  FailureReason   TEXT     null unless Failed
  ResponseTimeMs  INT
  RecordedAt      DATETIME (UTC)
```

Why a normalised `SearchSuppliers` table instead of a JSON column: it is
queryable and keeps the model relational; a JSON column would be an
optimisation we cannot justify yet.

Why record `NoResponse` rows: the brief says failures must be recorded, and
"never answered within the budget" is the most important failure to see in
history.

### 1.6 History endpoint: server-side filtering, sorting and paging

`GET /api/history?startDate&endDate&suppliers=A&suppliers=B&fromLocation&toLocation&sortBy&sortDirection&page&pageSize`
returns `{ items, totalCount, page, pageSize }`.

Sorting and paging happen in SQL, not in the browser: a history table grows
without bound, so client-side paging would download everything on every visit.
Sort columns are whitelisted via an enum to prevent arbitrary expressions.

### 1.7 Cross-cutting

- Input validation at the API boundary returns RFC 7807 `ValidationProblem`.
  Domain invariants (date range order, non-empty locations) live in value
  objects so they cannot be bypassed.
- `AddProblemDetails()` + exception handler for consistent error bodies.
- Structured logging with a scope per `searchId`.
- CORS policy for the Angular dev origin only.
- OpenAPI document for the non-streaming endpoints.

### 1.8 Testing strategy

- NUnit + NSubstitute + `Microsoft.Extensions.TimeProvider.Testing`. Assertions use
  NUnit's constraint model (`Assert.That`), no third-party assertion library.
- Application layer: fan-out tested with scripted fake suppliers and a fake
  clock (quote ordering, failure isolation, timeout status, cancellation
  propagation to supplier tokens, status discrimination).
- Infrastructure: `SimulatedSupplier` with a seeded `Random`; repository
  against SQLite in-memory.
- Api: `WebApplicationFactory` integration test that reads the SSE stream end
  to end and asserts the event sequence and the persisted rows.
- Coverage target: >= 80 % line / branch on behavioural classes (POCOs excluded).

### 1.9 Angular (Part 1 + Part 2 client)

- Latest stable Angular, standalone components, signals for view state,
  Angular Material for form/table/paginator/sort.
- `SearchStreamClient` wraps `EventSource` in an RxJS `Observable`;
  teardown calls `close()`. The search component uses `switchMap`, which is
  *exactly* "a new search cancels the previous one".
- Results list uses `@for (... ; track quote.supplierName)` so re-sorting
  moves DOM nodes instead of recreating them – this is what prevents the
  flicker the brief warns about.
- History screen: Material table with server-driven `matSort` and
  `matPaginator`; filters in a reactive form; supplier multi-select populated
  from `GET /api/suppliers`.

---

## 2. Steps (one commit each)

### Server

| #  | Step                                                                 | Done |
|----|----------------------------------------------------------------------|------|
| 0  | Repo init, `.gitignore`, this plan, README skeleton                  | [x]  |
| 1  | Solution + 4 src projects + 4 test projects, `Directory.Build.props` (nullable, warnings as errors, implicit usings), builds green | [x] |
| 2  | Domain: `Route`, `DateRange` value objects; `Search`, `SupplierResponse` entities; `SearchStatus`, `ResponseOutcome` enums (tests for value-object invariants) | [x] |
| 3  | Application ports: `ISupplier`, `ISupplierCatalog`, `ISearchRepository`, `SearchRequest`, `SearchEvent` hierarchy | [ ] |
| 4  | Application: `SupplierFanOut` – TDD with `FakeTimeProvider` (ordering, isolation, timeout, cancellation) | [ ] |
| 5  | Application: `SearchRecorder` + `PriceSearch` use case composing fan-out and recording | [ ] |
| 6  | Infrastructure: `SimulatedSupplier`, `SupplierProfile`, `SupplierCatalog` with the seven profiles (tests with seeded `Random`) | [ ] |
| 7  | Infrastructure: `PriceHuntDbContext`, entity configurations, initial migration, `SearchRepository` (tests on SQLite in-memory) | [ ] |
| 8  | Api: DI composition, `GET /api/suppliers`, `GET /api/searches/stream` SSE endpoint, request validation | [ ] |
| 9  | Api: integration test reading the SSE stream end to end               | [ ] |
| 10 | Application + Api: history query (filter, whitelisted sort, paging) + `GET /api/history` + tests | [ ] |
| 11 | Cross-cutting: ProblemDetails, CORS, logging scopes, OpenAPI; `dotnet run` smoke test | [ ] |
| 12 | README: how to run, streaming rationale, schema, trade-offs, AI usage | [ ] |

### Client

| #  | Step                                                                 | Done |
|----|----------------------------------------------------------------------|------|
| 13 | Angular workspace, Material, folder structure, proxy to API          | [ ] |
| 14 | `SuppliersClient`, `SearchStreamClient` (EventSource → Observable) + unit tests | [ ] |
| 15 | Search screen: form, progressive sorted list, progress + final state, switchMap cancellation | [ ] |
| 16 | History screen: filters, server-driven sort/paging table             | [ ] |
| 17 | Polish: routing/nav, error states, README client section             | [ ] |

---

## 3. Working agreements

- Test-first for every behavioural class (red → green → refactor).
- Every behavioural class has an interface and is consumed through it.
- No filler names (`Manager`, `Service`, `Helper`); names say what the thing does.
- Run `code-review` on each step's diff before committing.
- Nothing is pushed unless asked.
