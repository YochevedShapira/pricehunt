import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import {
  HistoryEntry,
  HistoryPage,
  HistoryPaging,
  HistorySort,
  isHistorySortColumn,
} from '../../../core/api/history';

/**
 * Presentational: the history rows with sortable headers and a paginator.
 * Sorting and paging are server-driven, so this component only reports the
 * user's intent and renders whatever page it is given.
 */
@Component({
  selector: 'app-history-table',
  imports: [
    CurrencyPipe,
    DatePipe,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatProgressBarModule,
  ],
  templateUrl: './history-table.html',
  styleUrl: './history-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoryTable {
  static readonly PAGE_SIZE_OPTIONS = [10, 20, 50];

  readonly page = input.required<HistoryPage | null>();
  readonly sort = input.required<HistorySort>();
  readonly loading = input(false);
  /** Shown instead of the empty message when no supplier is selected. */
  readonly needsSupplierSelection = input(false);

  readonly sortChange = output<HistorySort>();
  readonly pageChange = output<HistoryPaging>();

  protected readonly columns = ['date', 'route', 'supplier', 'price', 'responseTime'];
  protected readonly pageSizeOptions = HistoryTable.PAGE_SIZE_OPTIONS;
  protected readonly rows = computed(() => this.page()?.items ?? []);

  protected readonly trackRow = (_: number, row: HistoryEntry): string =>
    `${row.searchId}:${row.supplierName}`;

  protected onSort(sort: Sort): void {
    if (
      isHistorySortColumn(sort.active) &&
      (sort.direction === 'asc' || sort.direction === 'desc')
    ) {
      this.sortChange.emit({ column: sort.active, direction: sort.direction });
    }
  }

  protected onPage(event: PageEvent): void {
    this.pageChange.emit({ page: event.pageIndex + 1, pageSize: event.pageSize });
  }

  protected seconds(ms: number): string {
    return `${(ms / 1000).toFixed(2)} s`;
  }
}
