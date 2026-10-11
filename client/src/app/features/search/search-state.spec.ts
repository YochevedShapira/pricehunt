import { SearchRequest } from '../../core/api/search-events';
import {
  INITIAL_SEARCH_STATE,
  SearchState,
  applyEvent,
  beginSearch,
  cancelSearch,
  failSearch,
  progressOf,
  rankPrices,
} from './search-state';

const REQUEST: SearchRequest = {
  fromLocation: 'Haifa',
  toLocation: 'Rotterdam',
  fromDate: '2026-03-01',
  toDate: '2026-03-10',
  suppliers: [],
};
const SUPPLIERS = ['Oceanic', 'BlueWave', 'Nordic'];

function searching(): SearchState {
  return beginSearch(INITIAL_SEARCH_STATE, REQUEST, SUPPLIERS);
}

describe('search state', () => {
  describe('beginSearch', () => {
    it('marks every requested supplier as pending', () => {
      const state = searching();

      expect(state.suppliers.map((s) => s.outcome)).toEqual(['pending', 'pending', 'pending']);
    });

    it('numbers searches from one', () => {
      expect(searching().searchNo).toBe(1);
    });

    it('describes the route', () => {
      expect(searching().route).toBe('Haifa → Rotterdam');
    });

    it('remembers that a running search was replaced', () => {
      const second = beginSearch(searching(), REQUEST, SUPPLIERS);

      expect(second.replacedRunningSearch).toBe(true);
    });

    it('does not flag a replacement when the previous search had ended', () => {
      const ended = applyEvent(searching(), {
        type: 'ended',
        status: 'Completed',
        respondedCount: 0,
      });

      expect(beginSearch(ended, REQUEST, SUPPLIERS).replacedRunningSearch).toBe(false);
    });
  });

  describe('applyEvent', () => {
    it('records a price against its supplier', () => {
      const state = applyEvent(searching(), {
        type: 'price',
        supplierName: 'BlueWave',
        amount: 900,
        currency: 'USD',
        responseTimeMs: 1200,
      });

      expect(state.suppliers[1]).toEqual({
        name: 'BlueWave',
        outcome: 'priced',
        amount: 900,
        currency: 'USD',
        responseTimeMs: 1200,
      });
    });

    it('records a failure against its supplier', () => {
      const state = applyEvent(searching(), {
        type: 'failed',
        supplierName: 'Nordic',
        reason: 'Down',
        responseTimeMs: 300,
      });

      expect(state.suppliers[2].outcome).toBe('failed');
    });

    it('sets the final phase from the ended event', () => {
      const state = applyEvent(searching(), {
        type: 'ended',
        status: 'TimedOut',
        respondedCount: 1,
      });

      expect(state.phase).toBe('TimedOut');
    });

    it('marks suppliers still pending at the end as no response', () => {
      const state = applyEvent(searching(), {
        type: 'ended',
        status: 'TimedOut',
        respondedCount: 0,
      });

      expect(state.suppliers.map((s) => s.outcome)).toEqual([
        'noResponse',
        'noResponse',
        'noResponse',
      ]);
    });
  });

  describe('cancelSearch', () => {
    it('marks pending suppliers as cancelled', () => {
      const state = cancelSearch(searching());

      expect(state.suppliers[0].outcome).toBe('cancelled');
    });

    it('sets the phase to Cancelled', () => {
      expect(cancelSearch(searching()).phase).toBe('Cancelled');
    });

    it('leaves an ended search untouched', () => {
      const ended = applyEvent(searching(), {
        type: 'ended',
        status: 'Completed',
        respondedCount: 0,
      });

      expect(cancelSearch(ended)).toBe(ended);
    });
  });

  describe('failSearch', () => {
    it('keeps the error message', () => {
      expect(failSearch(searching(), 'Lost connection').error).toBe('Lost connection');
    });
  });

  describe('rankPrices', () => {
    const priced = [applyEvent].reduce(
      (state) =>
        [
          { name: 'Oceanic', amount: 1500 },
          { name: 'BlueWave', amount: 900 },
          { name: 'Nordic', amount: 1200 },
        ].reduce(
          (s, p) =>
            applyEvent(s, {
              type: 'price',
              supplierName: p.name,
              amount: p.amount,
              currency: 'USD',
              responseTimeMs: 100,
            }),
          state,
        ),
      searching(),
    );

    it('orders cheapest first', () => {
      expect(rankPrices(priced).map((r) => r.supplierName)).toEqual([
        'BlueWave',
        'Nordic',
        'Oceanic',
      ]);
    });

    it('numbers the ranks from one', () => {
      expect(rankPrices(priced).map((r) => r.rank)).toEqual([1, 2, 3]);
    });

    it('measures each price against the cheapest', () => {
      expect(rankPrices(priced).map((r) => r.aboveCheapest)).toEqual([0, 300, 600]);
    });

    it('ignores suppliers without a price', () => {
      expect(rankPrices(searching())).toEqual([]);
    });
  });

  describe('progressOf', () => {
    it('counts priced and failed suppliers as responded', () => {
      let state = applyEvent(searching(), {
        type: 'price',
        supplierName: 'Oceanic',
        amount: 1,
        currency: 'USD',
        responseTimeMs: 1,
      });
      state = applyEvent(state, {
        type: 'failed',
        supplierName: 'Nordic',
        reason: 'Down',
        responseTimeMs: 1,
      });

      expect(progressOf(state)).toEqual({ responded: 2, total: 3, percent: 67 });
    });

    it('is zero before a search', () => {
      expect(progressOf(INITIAL_SEARCH_STATE)).toEqual({ responded: 0, total: 0, percent: 0 });
    });
  });
});
