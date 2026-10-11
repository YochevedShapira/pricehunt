import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Observable, catchError, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';
import {
  EMPTY_HISTORY_FILTERS,
  HistoryFilters,
  HistoryPage,
  HistoryPaging,
  HistoryQuery,
  HistorySort,
} from '../../core/api/history';
import { HistoryClient } from '../../core/api/history-client';
import { SuppliersClient } from '../../core/api/suppliers-client';
import { HistoryFiltersPanel } from './history-filters/history-filters';
import { HistoryTable } from './history-table/history-table';

interface HistoryResult {
  readonly page: HistoryPage | null;
  readonly error: string | null;
}

const NO_RESULT: HistoryResult = { page: null, error: null };
const DEFAULT_SORT: HistorySort = { column: 'date', direction: 'desc' };
const DEFAULT_PAGING: HistoryPaging = { page: 1, pageSize: 20 };

/**
 * Container for the history screen. Filters, sort and paging are three
 * signals combined into one query; every change to the query becomes a
 * request through `switchMap`, so a stale response can never overwrite a
 * newer one.
 */
@Component({
  selector: 'app-history-page',
  imports: [HistoryFiltersPanel, HistoryTable],
  templateUrl: './history-page.html',
  styleUrl: './history-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoryPageComponent {
  private readonly suppliersClient = inject(SuppliersClient);
  private readonly historyClient = inject(HistoryClient);

  protected readonly suppliers = toSignal(this.suppliersClient.list(), { initialValue: [] });

  /** Null until the user changes something: "all suppliers, no other filter". */
  private readonly filters = signal<HistoryFilters | null>(null);
  protected readonly sort = signal<HistorySort>(DEFAULT_SORT);
  private readonly paging = signal<HistoryPaging>(DEFAULT_PAGING);
  protected readonly loading = signal(false);

  private readonly allSupplierNames = computed(() => this.suppliers().map((s) => s.name));

  protected readonly selectedSuppliers = computed(
    () => this.filters()?.suppliers ?? this.allSupplierNames(),
  );

  protected readonly needsSupplierSelection = computed(
    () => this.filters() !== null && this.selectedSuppliers().length === 0,
  );

  /** Null means there is nothing to ask the server for. */
  private readonly query = computed<HistoryQuery | null>(() => {
    if (this.needsSupplierSelection()) {
      return null;
    }
    const filters = this.filters() ?? EMPTY_HISTORY_FILTERS;
    const selected = this.selectedSuppliers();

    return {
      ...filters,
      // Everything selected is the server's default, so send nothing.
      suppliers: selected.length === this.allSupplierNames().length ? [] : selected,
      ...this.sort(),
      ...this.paging(),
    };
  });

  protected readonly result = toSignal(
    toObservable(this.query).pipe(
      distinctUntilChanged(sameQuery),
      switchMap((query) => (query ? this.load(query) : of(NO_RESULT))),
    ),
    { initialValue: NO_RESULT },
  );

  protected onFiltersChange(filters: HistoryFilters): void {
    this.filters.set(filters);
    this.paging.update((p) => ({ ...p, page: 1 }));
  }

  protected onReset(): void {
    this.filters.set(null);
    this.sort.set(DEFAULT_SORT);
    this.paging.set(DEFAULT_PAGING);
  }

  protected onSortChange(sort: HistorySort): void {
    this.sort.set(sort);
    this.paging.update((p) => ({ ...p, page: 1 }));
  }

  protected onPageChange(paging: HistoryPaging): void {
    this.paging.set(paging);
  }

  private load(query: HistoryQuery): Observable<HistoryResult> {
    this.loading.set(true);

    return this.historyClient.read(query).pipe(
      map((page): HistoryResult => ({ page, error: null })),
      catchError((error: unknown) => of({ page: null, error: describe(error) })),
      tap(() => this.loading.set(false)),
    );
  }
}

function sameQuery(a: HistoryQuery | null, b: HistoryQuery | null): boolean {
  return JSON.stringify(a) === JSON.stringify(b);
}

function describe(error: unknown): string {
  return error instanceof Error ? error.message : 'The history could not be loaded.';
}
