import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { EMPTY, Observable, Subject, catchError, switchMap, tap, timer } from 'rxjs';
import { SearchEvent, SearchRequest } from '../../core/api/search-events';
import { SearchStreamClient } from '../../core/api/search-stream-client';
import { SuppliersClient } from '../../core/api/suppliers-client';
import { ResultsList } from './results-list/results-list';
import { SearchForm } from './search-form/search-form';
import { SearchProgressPanel } from './search-progress/search-progress';
import {
  INITIAL_SEARCH_STATE,
  SearchPhase,
  applyEvent,
  beginSearch,
  cancelSearch,
  failSearch,
  progressOf,
  rankPrices,
} from './search-state';

/**
 * Container for the search screen. Owns the state and the stream; the three
 * components it renders are presentational. "A new search cancels the
 * previous one" is the `switchMap` below: subscribing to the new stream
 * unsubscribes the old one, which closes its connection.
 */
@Component({
  selector: 'app-search-page',
  imports: [SearchForm, SearchProgressPanel, ResultsList],
  templateUrl: './search-page.html',
  styleUrl: './search-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchPage {
  private static readonly ELAPSED_TICK_MS = 100;

  private readonly suppliersClient = inject(SuppliersClient);
  private readonly streamClient = inject(SearchStreamClient);
  private readonly destroyRef = inject(DestroyRef);

  /** A request starts a search; null cancels the running one. */
  private readonly commands = new Subject<SearchRequest | null>();

  protected readonly suppliers = toSignal(this.suppliersClient.list(), { initialValue: [] });
  protected readonly state = signal(INITIAL_SEARCH_STATE);
  protected readonly elapsedMs = signal(0);

  protected readonly searching = computed(() => this.state().phase === 'searching');

  /** The phase while a search has run; null before the first one. */
  protected readonly activePhase = computed<Exclude<SearchPhase, 'idle'> | null>(() => {
    const phase = this.state().phase;
    return phase === 'idle' ? null : phase;
  });
  protected readonly hasSearched = computed(() => this.state().phase !== 'idle');
  protected readonly results = computed(() => rankPrices(this.state()));
  protected readonly progress = computed(() => progressOf(this.state()));

  constructor() {
    this.commands
      .pipe(
        switchMap((request) => (request ? this.run(request) : EMPTY)),
        takeUntilDestroyed(),
      )
      .subscribe();
  }

  protected search(request: SearchRequest): void {
    this.commands.next(request);
  }

  protected cancel(): void {
    this.commands.next(null);
    this.state.update(cancelSearch);
  }

  private run(request: SearchRequest): Observable<SearchEvent> {
    const names =
      request.suppliers.length > 0 ? request.suppliers : this.suppliers().map((s) => s.name);
    this.state.update((s) => beginSearch(s, request, names));
    const stopwatch = this.startStopwatch();

    return this.streamClient.search(request).pipe(
      tap((event) => this.state.update((s) => applyEvent(s, event))),
      catchError((error: unknown) => {
        this.state.update((s) => failSearch(s, describe(error)));
        return EMPTY;
      }),
      tap({ finalize: () => stopwatch.unsubscribe() }),
    );
  }

  private startStopwatch() {
    const startedAt = Date.now();
    this.elapsedMs.set(0);

    return timer(0, SearchPage.ELAPSED_TICK_MS)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.elapsedMs.set(Date.now() - startedAt));
  }
}

function describe(error: unknown): string {
  return error instanceof Error ? error.message : 'The search failed unexpectedly.';
}
