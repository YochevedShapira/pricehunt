import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { Observable, Subject } from 'rxjs';
import { SearchEvent, SearchRequest } from '../../core/api/search-events';
import { SearchStreamClient } from '../../core/api/search-stream-client';
import { SearchPage } from './search-page';

/** Stands in for the stream client; the test pushes events and watches closes. */
class FakeStreamClient {
  readonly requests: SearchRequest[] = [];
  readonly events = new Subject<SearchEvent>();
  closed = 0;

  search(request: SearchRequest): Observable<SearchEvent> {
    this.requests.push(request);
    return new Observable<SearchEvent>((subscriber) => {
      const subscription = this.events.subscribe(subscriber);
      return () => {
        this.closed++;
        subscription.unsubscribe();
      };
    });
  }
}

const REQUEST: SearchRequest = {
  fromLocation: 'Haifa',
  toLocation: 'Rotterdam',
  fromDate: '2026-03-01',
  toDate: '2026-03-10',
  suppliers: ['Oceanic', 'BlueWave'],
};

describe('SearchPage', () => {
  let fixture: ComponentFixture<SearchPage>;
  let stream: FakeStreamClient;

  beforeEach(async () => {
    stream = new FakeStreamClient();
    await TestBed.configureTestingModule({
      imports: [SearchPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNativeDateAdapter(),
        { provide: SearchStreamClient, useValue: stream },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SearchPage);
    await fixture.whenStable();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/suppliers')
      .flush([{ name: 'Oceanic' }, { name: 'BlueWave' }]);
    await fixture.whenStable();
  });

  it('starts with the empty state', () => {
    expect(text()).toContain('Choose a route and dates');
  });

  it('opens a stream when a search is requested', () => {
    startSearch();

    expect(stream.requests).toEqual([REQUEST]);
  });

  it('shows the progress once a search runs', async () => {
    startSearch();
    await fixture.whenStable();

    expect(text()).toContain('0 of 2 suppliers responded');
  });

  it('lists prices cheapest first as they arrive', async () => {
    startSearch();
    stream.events.next(price('Oceanic', 1500));
    stream.events.next(price('BlueWave', 900));
    await fixture.whenStable();

    expect(rankedNames()).toEqual(['BlueWave', 'Oceanic']);
  });

  it('shows the final state from the ended event', async () => {
    startSearch();
    stream.events.next({ type: 'ended', status: 'TimedOut', respondedCount: 0 });
    await fixture.whenStable();

    expect(text()).toContain('Timed out');
  });

  it('closes the previous stream when a new search starts', () => {
    startSearch();

    startSearch();

    expect(stream.closed).toBe(1);
  });

  it('keeps only the new search after a replacement', async () => {
    startSearch();
    stream.events.next(price('Oceanic', 1500));
    startSearch();
    await fixture.whenStable();

    expect(rankedNames()).toEqual([]);
  });

  it('closes the stream when the user cancels', async () => {
    startSearch();

    fixture.componentInstance['cancel']();
    await fixture.whenStable();

    expect([stream.closed, text()]).toEqual([1, expect.stringContaining('Cancelled')]);
  });

  function startSearch(): void {
    fixture.componentInstance['search'](REQUEST);
  }

  function price(supplierName: string, amount: number): SearchEvent {
    return { type: 'price', supplierName, amount, currency: 'USD', responseTimeMs: 100 };
  }

  function text(): string {
    const element: HTMLElement = fixture.nativeElement;
    return element.textContent ?? '';
  }

  function rankedNames(): string[] {
    const element: HTMLElement = fixture.nativeElement;
    return Array.from(element.querySelectorAll('.results__name')).map(
      (n) => n.textContent?.trim() ?? '',
    );
  }
});
