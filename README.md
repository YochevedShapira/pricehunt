# PriceHunt

Live shipping-price search across seven simulated suppliers, streamed to the
browser as prices arrive, with a persisted and filterable price history.

- **Server:** .NET 10, ASP.NET Core minimal APIs, EF Core 10 + SQLite.
- **Client:** Angular (coming in the next steps).

The step-by-step build plan and every design decision with its rejected
alternatives are in [docs/PLAN.md](docs/PLAN.md).

## How to run

Prerequisite: the .NET 10 SDK. Nothing else to install.

```
cd server
dotnet run --project src/PriceHunt.Api
```

The API listens on `http://localhost:5084`. The SQLite database file is
created next to the project on first run by applying the migrations.

Try it:

```
curl http://localhost:5084/api/suppliers
curl -N "http://localhost:5084/api/searches/stream?fromLocation=Haifa&toLocation=Rotterdam&fromDate=2026-03-01&toDate=2026-03-10"
curl "http://localhost:5084/api/history?sortBy=price&sortDirection=asc&pageSize=10"
```

Tests (210, no database or network setup needed):

```
cd server
dotnet test
```

## Endpoints

| Endpoint | Purpose |
|---|---|
| `GET /api/suppliers` | The supplier catalog, so the client never hard-codes names. |
| `GET /api/searches/stream?fromLocation&toLocation&fromDate&toDate&suppliers=A&suppliers=B` | Runs a search and streams Server-Sent Events. No `suppliers` means all seven. |
| `GET /api/history?startDate&endDate&suppliers&fromLocation&toLocation&sortBy&sortDirection&page&pageSize` | One page of past responses, filtered, sorted and paged in SQL. |
| `GET /health` | Liveness plus a database check. |
| `GET /openapi/v1.json` | OpenAPI document (Development only). |

Invalid input returns a `400` problem-details body that names every bad
parameter at once.

### The stream

```
event: started   data: {"searchId":"…","supplierCount":7}
event: price     data: {"supplierName":"…","amount":1748.94,"currency":"USD","responseTimeMs":1423}
event: failed    data: {"supplierName":"…","reason":"…","responseTimeMs":3116}
event: ended     data: {"status":"TimedOut","respondedCount":6}
```

`status` is `Completed`, `TimedOut` or `Cancelled`. The client should close
the connection after `ended`, because browsers reconnect to a closed SSE
stream automatically.

## Streaming: why Server-Sent Events

The data flows one way, from server to browser, after the initial request.
SSE is the HTTP standard for exactly that, and it answers the brief's
requirements directly:

- **Stream each result as it arrives.** One long-lived response; each
  supplier result is one flushed event. .NET 10's `TypedResults.ServerSentEvents`
  sets the headers, disables buffering and writes the wire format.
- **Cancellation.** Closing the connection fires the request's cancellation
  token. That token is the one handed to every supplier call, so a closed tab
  or a new search stops the work in progress. No second protocol is needed.
- **Named events.** `started`, `price`, `failed`, `ended` are SSE event
  types, which the browser's built-in `EventSource` dispatches by name.

Rejected: **SignalR** (two-way machinery for a one-way flow, plus a client
library), **chunked JSON lines** (no standard framing, hand-written parser),
**WebSockets** (lowest level, everything by hand).

Trade-off: `EventSource` is GET-only, so the search parameters travel in the
query string. The search is idempotent from the client's point of view and the
parameters are small, so this is acceptable.

## How a search runs

```
Browser ──GET /api/searches/stream──▶ SearchEndpoints
                                        │ parse + validate
                                        ▼
                                     PriceSearch (Application)
                                        │ creates the Search aggregate, saves it
                                        │ runs SupplierFanOut with the 6 s timeout
                                        ▼
                                     SupplierFanOut
                                        │ one task per supplier ──▶ Channel ──▶ single reader
                                        ▼
                                     each outcome: record on aggregate → save → yield event
                                        ▼
                                     end: Completed / TimedOut / Cancelled → save
```

- Each supplier runs as its own task writing into a `System.Threading.Channels`
  channel. The single reader yields outcomes in arrival order.
- The timeout and the request's cancellation are linked into one token that
  every supplier receives. When it fires, the channel closes, so even a
  supplier that ignores cancellation cannot keep the stream open, and a late
  result is dropped.
- A supplier that throws becomes a `failed` event for that supplier only.
- The final status comes from two facts: did the caller's token fire
  (Cancelled), and did every supplier answer (Completed, otherwise TimedOut).
  The final save ignores the caller's token, so a cancelled search is always
  recorded as such. This was verified by disconnecting a real client and
  reading the database.
- Every outcome is saved before it is sent, so what the client sees is already
  on disk.

## Database

SQLite through EF Core, schema created by migrations at startup. Migrations
rather than `EnsureCreated()` because they can evolve the schema later; a test
fails if the model changes without a new migration.

```
Searches
  Id                  GUID (v7, time-ordered)   PK
  FromLocation, ToLocation            Route value object (complex type)
  FromDate, ToDate                    DateRange value object (complex type)
  RequestedSuppliers  JSON array of names
  StartedAt, EndedAt  UTC            indexed on StartedAt
  Status              Running | Completed | TimedOut | Cancelled

SupplierResponses                     one row per supplier per search
  Id                  PK autoincrement
  SearchId            FK → Searches, cascade      indexed
  SupplierName                                    indexed with RecordedAt
  Outcome             Priced | Failed | NoResponse
  PriceMinorUnits, PriceCurrency      Money value object, null unless Priced
  FailureReason       null unless Failed
  ResponseTimeMs
  RecordedAt          UTC
```

Why these shapes:

- **Money in integer cents, durations in milliseconds.** SQLite stores
  `decimal` as text, which does not sort numerically, and `double` is the wrong
  type for money. Integers sort correctly in SQL and lose nothing.
- **UTC `DateTime` columns, `DateTimeOffset` in the domain.** The SQLite
  provider cannot translate `DateTimeOffset` comparisons; a value converter
  bridges the two so the domain does not bend to the database.
- **Requested suppliers as JSON.** The list belongs to the search and is never
  queried alone; a join table would add a join for no query we have.
- **A `NoResponse` row for suppliers that never answered.** The brief asks for
  failures to be recorded, and "never answered within the limit" is the most
  useful failure to see in history.

## Architecture

Clean Architecture in four projects; dependencies point inward and are
enforced by project references.

| Project | Contains | Depends on |
|---|---|---|
| `PriceHunt.Domain` | `Search` aggregate, `SupplierResponse`, value objects `Route`, `DateRange`, `Money` | nothing |
| `PriceHunt.Application` | `PriceSearch` use case, `SupplierFanOut`, the ports `ISupplier`, `ISupplierCatalog`, `ISearchRepository`, `IHistoryReader` | Domain |
| `PriceHunt.Infrastructure` | `SimulatedSupplier` (one class, seven profiles), EF Core context, migrations, repository, history reader | Application |
| `PriceHunt.Api` | Endpoints, query parsers, SSE mapping, DI composition, settings | Application, Infrastructure |

Points worth knowing:

- **The `Search` aggregate owns the rules.** A supplier must have been
  requested, may answer once, nothing is recorded after the end, and ending
  the search writes a `NoResponse` for every supplier that never answered.
- **Time is injected.** All delays and the timeout go through `TimeProvider`,
  so the fan-out and the simulated suppliers are tested with a fake clock in
  milliseconds, with no sleeps.
- **Suppliers are a port.** The simulator is one adapter; an HTTP client for a
  real carrier would be another, registered the same way.
- **Configuration stays in the host.** The timeout is `Search:Timeout` in
  `appsettings.json`; the application layer receives a plain options object and
  knows nothing about configuration keys.
- **Validation at the edge, invariants in the domain.** The parsers produce
  friendly messages; the value objects still reject bad data if bypassed.

## Testing

- **Domain:** value-object and aggregate invariants.
- **Application:** fan-out and use case with scripted suppliers and
  `FakeTimeProvider`: arrival order, failure isolation, timeout, cancellation
  propagation, late results, early consumer exit.
- **Infrastructure:** simulated supplier with scripted random draws; repository
  and history reader against in-memory SQLite created by the real migrations;
  a model-snapshot check for the migrations.
- **Api:** parsers; in-process integration tests that read the real SSE stream
  with the BCL `SseParser`, including a client disconnect that must end as
  Cancelled, not TimedOut.

## What I would do differently with more time

- Run migrations as a deployment step rather than at startup, so several
  instances cannot race on the schema.
- Add authentication, rate limiting and HTTPS redirection, none of which the
  brief asks for.
- Replace `Random.Shared` in the simulator with a seedable source per run for
  reproducible demos.
- Expose the history filters' supplier list validation (an unknown name
  currently returns zero rows rather than a 400).
- Add `Task.WhenEach` as a lighter fan-in if the project ever drops Channels.

## AI usage

I used Claude Code as a pair programmer for the whole project, with a
deliberate workflow: I made every design decision, the tool produced the
first draft of each step, and I reviewed and corrected before committing.

- **Design.** I asked for comparison tables with pros, cons and references
  for the streaming transport (SSE vs SignalR vs chunked JSON vs
  WebSockets), the solution structure (Clean, Onion, Hexagonal, Vertical
  Slice, N-tier) and the fan-in mechanism (Channels, WhenAny, WhenEach,
  Rx.NET, Dataflow). I chose SSE, Clean Architecture and Channels and can
  defend each choice against its alternatives.
- **Implementation.** Each of the 12 server steps was generated test-first
  (NUnit + NSubstitute + FakeTimeProvider), reviewed by me, and committed
  separately so the history shows the order of construction.
- **Corrections I drove.** Replacing a hand-driven async enumerator with
  `await foreach` plus `finally` for cancellation; renaming "quote" to
  "price" to match the brief's vocabulary; moving the search timeout from a
  constant into `appsettings.json` with validation on startup; dropping a
  planned `SearchRecorder` class as needless indirection.
- **Verification.** The tool ran the test suite, generated the EF Core
  migration, and performed the smoke tests, including a real client
  disconnect to confirm the search is recorded as Cancelled.
- **Documentation.** The plan and this README were drafted with the tool
  and edited by me.

The git history shows the work in the order it was built, one step per commit.
