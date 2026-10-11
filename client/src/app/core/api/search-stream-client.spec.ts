import { TestBed } from '@angular/core/testing';
import { EVENT_SOURCE_FACTORY } from './event-source-factory';
import { SearchEvent, SearchRequest } from './search-events';
import { SearchStreamClient } from './search-stream-client';

/** Minimal stand-in for the browser EventSource the test can drive. */
class FakeEventSource {
  readonly listeners = new Map<string, (event: MessageEvent<string>) => void>();
  onerror: (() => void) | null = null;
  closed = false;

  constructor(readonly url: string) {}

  addEventListener(type: string, listener: (event: MessageEvent<string>) => void): void {
    this.listeners.set(type, listener);
  }

  close(): void {
    this.closed = true;
  }

  emit(type: string, data: object): void {
    this.listeners.get(type)!(new MessageEvent(type, { data: JSON.stringify(data) }));
  }
}

const REQUEST: SearchRequest = {
  fromLocation: 'Haifa',
  toLocation: 'Rotterdam',
  fromDate: '2026-03-01',
  toDate: '2026-03-10',
  suppliers: ['Oceanic', 'BlueWave'],
};

describe('SearchStreamClient', () => {
  let client: SearchStreamClient;
  let source: FakeEventSource | undefined;

  beforeEach(() => {
    source = undefined;
    TestBed.configureTestingModule({
      providers: [
        {
          provide: EVENT_SOURCE_FACTORY,
          useValue: (url: string) => (source = new FakeEventSource(url)),
        },
      ],
    });
    client = TestBed.inject(SearchStreamClient);
  });

  it('opens the stream with the search parameters in the query string', () => {
    client.search(REQUEST).subscribe();

    expect(source!.url).toBe(
      '/api/searches/stream?fromLocation=Haifa&toLocation=Rotterdam&fromDate=2026-03-01&toDate=2026-03-10' +
        '&suppliers=Oceanic&suppliers=BlueWave',
    );
  });

  it('omits the suppliers parameter when every supplier is wanted', () => {
    client.search({ ...REQUEST, suppliers: [] }).subscribe();

    expect(source!.url).not.toContain('suppliers');
  });

  it('does not open a connection until subscribed', () => {
    client.search(REQUEST);

    expect(source).toBeUndefined();
  });

  it('emits each server event with its type', () => {
    const received: SearchEvent[] = [];
    client.search(REQUEST).subscribe((e) => received.push(e));

    source!.emit('started', { searchId: 'abc', supplierCount: 2 });
    source!.emit('price', {
      supplierName: 'Oceanic',
      amount: 1000,
      currency: 'USD',
      responseTimeMs: 50,
    });

    expect(received).toEqual([
      { type: 'started', searchId: 'abc', supplierCount: 2 },
      { type: 'price', supplierName: 'Oceanic', amount: 1000, currency: 'USD', responseTimeMs: 50 },
    ]);
  });

  it('completes after the ended event', () => {
    let completed = false;
    client.search(REQUEST).subscribe({ complete: () => (completed = true) });

    source!.emit('ended', { status: 'Completed', respondedCount: 2 });

    expect(completed).toBe(true);
  });

  it('closes the connection after the ended event so the browser does not reconnect', () => {
    client.search(REQUEST).subscribe();

    source!.emit('ended', { status: 'Completed', respondedCount: 2 });

    expect(source!.closed).toBe(true);
  });

  it('closes the connection when unsubscribed', () => {
    const subscription = client.search(REQUEST).subscribe();

    subscription.unsubscribe();

    expect(source!.closed).toBe(true);
  });

  it('errors when a message does not match the event contract', () => {
    let error: unknown;
    client.search(REQUEST).subscribe({ error: (e) => (error = e) });

    source!.emit('price', { supplierName: 'Oceanic' });

    expect(error).toBeInstanceOf(Error);
  });

  it('errors when the connection fails', () => {
    let error: Error | undefined;
    client.search(REQUEST).subscribe({ error: (e) => (error = e) });

    source!.onerror!();

    expect(error?.message).toContain('interrupted');
  });
});
