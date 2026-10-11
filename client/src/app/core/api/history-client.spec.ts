import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HistoryPage, HistoryQuery } from './history';
import { HistoryClient } from './history-client';

const QUERY: HistoryQuery = {
  startDate: '2026-03-01',
  endDate: '2026-03-10',
  suppliers: ['Oceanic', 'BlueWave'],
  fromLocation: ' Haifa ',
  toLocation: '',
  column: 'price',
  direction: 'asc',
  page: 2,
  pageSize: 20,
};

const PAGE: HistoryPage = {
  items: [
    {
      searchId: 'abc',
      date: '2026-03-01T10:00:00+00:00',
      fromLocation: 'Haifa',
      toLocation: 'Rotterdam',
      supplierName: 'Oceanic',
      outcome: 'Priced',
      amount: 1000,
      currency: 'USD',
      responseTimeMs: 1200,
    },
  ],
  totalCount: 1,
  page: 2,
  pageSize: 20,
};

describe('HistoryClient', () => {
  let client: HistoryClient;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    client = TestBed.inject(HistoryClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the sort and paging parameters', () => {
    client.read(QUERY).subscribe();

    const params = http.expectOne((r) => r.url === '/api/history').request.params;
    expect([
      params.get('sortBy'),
      params.get('sortDirection'),
      params.get('page'),
      params.get('pageSize'),
    ]).toEqual(['price', 'asc', '2', '20']);
  });

  it('sends each supplier as a repeated parameter', () => {
    client.read(QUERY).subscribe();

    const params = http.expectOne((r) => r.url === '/api/history').request.params;
    expect(params.getAll('suppliers')).toEqual(['Oceanic', 'BlueWave']);
  });

  it('trims location filters and omits empty ones', () => {
    client.read(QUERY).subscribe();

    const params = http.expectOne((r) => r.url === '/api/history').request.params;
    expect([params.get('fromLocation'), params.has('toLocation')]).toEqual(['Haifa', false]);
  });

  it('omits absent dates', () => {
    client.read({ ...QUERY, startDate: null, endDate: null }).subscribe();

    const params = http.expectOne((r) => r.url === '/api/history').request.params;
    expect([params.has('startDate'), params.has('endDate')]).toEqual([false, false]);
  });

  it('returns the validated page', () => {
    let received: HistoryPage | undefined;
    client.read(QUERY).subscribe((p) => (received = p));

    http.expectOne((r) => r.url === '/api/history').flush(PAGE);

    expect(received).toEqual(PAGE);
  });

  it('errors when the response does not match the contract', () => {
    let error: unknown;
    client.read(QUERY).subscribe({ error: (e) => (error = e) });

    http.expectOne((r) => r.url === '/api/history').flush({ items: 'nope' });

    expect(error).toBeInstanceOf(Error);
  });
});
