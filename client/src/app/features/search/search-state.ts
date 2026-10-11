import { SearchEvent, SearchRequest, SearchStatus } from '../../core/api/search-events';

/**
 * Pure state of the search screen and the functions that move it from one
 * event to the next. No Angular here: this is the "reducer", tested on its
 * own, and the page component only wires it to the stream.
 */

export type SupplierOutcome = 'pending' | 'priced' | 'failed' | 'noResponse' | 'cancelled';

export interface SupplierStatus {
  readonly name: string;
  readonly outcome: SupplierOutcome;
  readonly amount?: number;
  readonly currency?: string;
  readonly responseTimeMs?: number;
}

export type SearchPhase = 'idle' | 'searching' | SearchStatus | 'error';

export interface SearchState {
  readonly phase: SearchPhase;
  /** Counts the searches started on this screen; shown as "Search #n". */
  readonly searchNo: number;
  readonly route: string;
  readonly suppliers: readonly SupplierStatus[];
  /** True when this search replaced one that was still running. */
  readonly replacedRunningSearch: boolean;
  readonly error?: string;
}

export const INITIAL_SEARCH_STATE: SearchState = {
  phase: 'idle',
  searchNo: 0,
  route: '',
  suppliers: [],
  replacedRunningSearch: false,
};

export function beginSearch(
  state: SearchState,
  request: SearchRequest,
  supplierNames: readonly string[],
): SearchState {
  return {
    phase: 'searching',
    searchNo: state.searchNo + 1,
    route: `${request.fromLocation} → ${request.toLocation}`,
    suppliers: supplierNames.map((name) => ({ name, outcome: 'pending' })),
    replacedRunningSearch: state.phase === 'searching',
  };
}

export function applyEvent(state: SearchState, event: SearchEvent): SearchState {
  switch (event.type) {
    case 'started':
      return state;
    case 'price':
      return withSupplier(state, event.supplierName, {
        outcome: 'priced',
        amount: event.amount,
        currency: event.currency,
        responseTimeMs: event.responseTimeMs,
      });
    case 'failed':
      return withSupplier(state, event.supplierName, {
        outcome: 'failed',
        responseTimeMs: event.responseTimeMs,
      });
    case 'ended':
      return {
        ...state,
        phase: event.status,
        suppliers: state.suppliers.map((s) =>
          s.outcome === 'pending' ? { ...s, outcome: 'noResponse' } : s,
        ),
      };
  }
}

function withSupplier(
  state: SearchState,
  name: string,
  change: Partial<SupplierStatus>,
): SearchState {
  return {
    ...state,
    suppliers: state.suppliers.map((s) => (s.name === name ? { ...s, ...change } : s)),
  };
}

export function cancelSearch(state: SearchState): SearchState {
  if (state.phase !== 'searching') {
    return state;
  }

  return {
    ...state,
    phase: 'Cancelled',
    suppliers: state.suppliers.map((s) =>
      s.outcome === 'pending' ? { ...s, outcome: 'cancelled' } : s,
    ),
  };
}

export function failSearch(state: SearchState, error: string): SearchState {
  return { ...cancelSearch(state), phase: 'error', error };
}

/** A priced supplier ranked against the cheapest one. */
export interface RankedPrice {
  readonly rank: number;
  readonly supplierName: string;
  readonly amount: number;
  readonly currency: string;
  readonly responseTimeMs: number;
  /** Difference to the cheapest price; 0 for the cheapest itself. */
  readonly aboveCheapest: number;
}

/** Cheapest first; ties keep supplier order so rows never swap arbitrarily. */
export function rankPrices(state: SearchState): RankedPrice[] {
  const priced = state.suppliers.filter(
    (s): s is SupplierStatus & { amount: number; currency: string; responseTimeMs: number } =>
      s.outcome === 'priced' &&
      s.amount !== undefined &&
      s.currency !== undefined &&
      s.responseTimeMs !== undefined,
  );
  const sorted = [...priced].sort((a, b) => a.amount - b.amount);
  const cheapest = sorted[0]?.amount ?? 0;

  return sorted.map((s, index) => ({
    rank: index + 1,
    supplierName: s.name,
    amount: s.amount,
    currency: s.currency,
    responseTimeMs: s.responseTimeMs,
    aboveCheapest: s.amount - cheapest,
  }));
}

export interface SearchProgress {
  readonly responded: number;
  readonly total: number;
  readonly percent: number;
}

export function progressOf(state: SearchState): SearchProgress {
  const total = state.suppliers.length;
  const responded = state.suppliers.filter(
    (s) => s.outcome === 'priced' || s.outcome === 'failed',
  ).length;

  return { responded, total, percent: total === 0 ? 0 : Math.round((responded / total) * 100) };
}
