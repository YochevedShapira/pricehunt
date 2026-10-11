import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { Observable, Subject } from 'rxjs';
import { HistoryPage, HistoryQuery } from '../../core/api/history';
import { HistoryClient } from '../../core/api/history-client';
import { HistoryPageComponent } from './history-page';

class FakeHistoryClient {
  readonly queries: HistoryQuery[] = [];
  readonly responses = new Subject<HistoryPage>();

  read(query: HistoryQuery): Observable<HistoryPage> {
    this.queries.push(query);
    return this.responses;
  }
}

const EMPTY_PAGE: HistoryPage = { items: [], totalCount: 0, page: 1, pageSize: 20 };

describe('HistoryPageComponent', () => {
  let fixture: ComponentFixture<HistoryPageComponent>;
  let client: FakeHistoryClient;

  beforeEach(async () => {
    client = new FakeHistoryClient();
    await TestBed.configureTestingModule({
      imports: [HistoryPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNativeDateAdapter(),
        { provide: HistoryClient, useValue: client },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HistoryPageComponent);
    await fixture.whenStable();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/suppliers')
      .flush([{ name: 'Oceanic' }, { name: 'BlueWave' }]);
    await fixture.whenStable();
  });

  it('loads the first page newest first on open', () => {
    expect(client.queries[0]).toMatchObject({
      column: 'date',
      direction: 'desc',
      page: 1,
      pageSize: 20,
      suppliers: [],
    });
  });

  it('does not repeat the request when the supplier catalog arrives', () => {
    expect(client.queries.length).toBe(1);
  });

  it('requests the new sort and returns to the first page', async () => {
    fixture.componentInstance['onPageChange']({ page: 3, pageSize: 20 });
    await fixture.whenStable();

    fixture.componentInstance['onSortChange']({ column: 'price', direction: 'asc' });
    await fixture.whenStable();

    expect(client.queries.at(-1)).toMatchObject({ column: 'price', direction: 'asc', page: 1 });
  });

  it('requests the chosen page', async () => {
    fixture.componentInstance['onPageChange']({ page: 2, pageSize: 10 });
    await fixture.whenStable();

    expect(client.queries.at(-1)).toMatchObject({ page: 2, pageSize: 10 });
  });

  it('sends only the selected suppliers when some are deselected', async () => {
    fixture.componentInstance['onFiltersChange']({
      startDate: null,
      endDate: null,
      suppliers: ['BlueWave'],
      fromLocation: '',
      toLocation: '',
    });
    await fixture.whenStable();

    expect(client.queries.at(-1)?.suppliers).toEqual(['BlueWave']);
  });

  it('does not query when no supplier is selected', async () => {
    const before = client.queries.length;

    fixture.componentInstance['onFiltersChange']({
      startDate: null,
      endDate: null,
      suppliers: [],
      fromLocation: '',
      toLocation: '',
    });
    await fixture.whenStable();

    expect([client.queries.length, text()]).toEqual([
      before,
      expect.stringContaining('Select at least one supplier'),
    ]);
  });

  it('renders the page it receives', async () => {
    client.responses.next({ ...EMPTY_PAGE, totalCount: 7 });
    await fixture.whenStable();

    expect(text()).toContain('No replies match these filters.');
  });

  function text(): string {
    const element: HTMLElement = fixture.nativeElement;
    return element.textContent ?? '';
  }
});
