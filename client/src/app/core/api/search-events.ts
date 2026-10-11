/**
 * The events of `GET /api/searches/stream`, one per SSE event name. A
 * discriminated union on `type`, so a `switch` over an event is exhaustive.
 */

export interface SearchStarted {
  readonly type: 'started';
  readonly searchId: string;
  readonly supplierCount: number;
}

export interface PriceReceived {
  readonly type: 'price';
  readonly supplierName: string;
  readonly amount: number;
  readonly currency: string;
  readonly responseTimeMs: number;
}

export interface SupplierFailed {
  readonly type: 'failed';
  readonly supplierName: string;
  readonly reason: string;
  readonly responseTimeMs: number;
}

export type SearchStatus = 'Completed' | 'TimedOut' | 'Cancelled';

export interface SearchEnded {
  readonly type: 'ended';
  readonly status: SearchStatus;
  readonly respondedCount: number;
}

export type SearchEvent = SearchStarted | PriceReceived | SupplierFailed | SearchEnded;

/** The event names the server sends, used to subscribe to the stream. */
export const SEARCH_EVENT_TYPES: readonly SearchEvent['type'][] = [
  'started',
  'price',
  'failed',
  'ended',
];

/** What the user asked for; becomes the query string of the stream URL. */
export interface SearchRequest {
  readonly fromLocation: string;
  readonly toLocation: string;
  readonly fromDate: string;
  readonly toDate: string;
  /** Empty means every supplier. */
  readonly suppliers: readonly string[];
}
