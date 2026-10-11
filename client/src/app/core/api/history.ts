import { z } from 'zod';

/** The contract of `GET /api/history`, validated at the boundary. */

export const historyOutcomeSchema = z.enum(['Priced', 'Failed', 'NoResponse']);

const historyEntrySchema = z.object({
  searchId: z.string(),
  date: z.string(),
  fromLocation: z.string(),
  toLocation: z.string(),
  supplierName: z.string(),
  outcome: historyOutcomeSchema,
  amount: z.number().nullable(),
  currency: z.string().nullable(),
  responseTimeMs: z.number().int().nonnegative(),
});

export const historyPageSchema = z.object({
  items: z.array(historyEntrySchema),
  totalCount: z.number().int().nonnegative(),
  page: z.number().int().positive(),
  pageSize: z.number().int().positive(),
});

export type HistoryOutcome = z.infer<typeof historyOutcomeSchema>;
export type HistoryEntry = z.infer<typeof historyEntrySchema>;
export type HistoryPage = z.infer<typeof historyPageSchema>;

/** Column names as the server accepts them in `sortBy`. */
export const HISTORY_SORT_COLUMNS = ['date', 'route', 'supplier', 'price', 'responseTime'] as const;
export type HistorySortColumn = (typeof HISTORY_SORT_COLUMNS)[number];
export type SortDirection = 'asc' | 'desc';

export function isHistorySortColumn(value: string): value is HistorySortColumn {
  return HISTORY_SORT_COLUMNS.some((column) => column === value);
}

/** What the user can filter by. Empty suppliers means every supplier. */
export interface HistoryFilters {
  readonly startDate: string | null;
  readonly endDate: string | null;
  readonly suppliers: readonly string[];
  readonly fromLocation: string;
  readonly toLocation: string;
}

export const EMPTY_HISTORY_FILTERS: HistoryFilters = {
  startDate: null,
  endDate: null,
  suppliers: [],
  fromLocation: '',
  toLocation: '',
};

export interface HistorySort {
  readonly column: HistorySortColumn;
  readonly direction: SortDirection;
}

export interface HistoryPaging {
  /** One-based, as the server counts pages. */
  readonly page: number;
  readonly pageSize: number;
}

export type HistoryQuery = HistoryFilters & HistorySort & HistoryPaging;
