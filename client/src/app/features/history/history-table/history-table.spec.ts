import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HistoryPage, HistoryPaging, HistorySort } from '../../../core/api/history';
import { HistoryTable } from './history-table';

const PAGE: HistoryPage = {
  items: [
    {
      searchId: 'a',
      date: '2026-03-01T10:00:00+00:00',
      fromLocation: 'Haifa',
      toLocation: 'Rotterdam',
      supplierName: 'Oceanic',
      outcome: 'Priced',
      amount: 1000,
      currency: 'USD',
      responseTimeMs: 1200,
    },
    {
      searchId: 'a',
      date: '2026-03-01T10:00:00+00:00',
      fromLocation: 'Haifa',
      toLocation: 'Rotterdam',
      supplierName: 'BlueWave',
      outcome: 'Failed',
      amount: null,
      currency: null,
      responseTimeMs: 300,
    },
  ],
  totalCount: 42,
  page: 2,
  pageSize: 20,
};

describe('HistoryTable', () => {
  let fixture: ComponentFixture<HistoryTable>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HistoryTable] }).compileComponents();
    fixture = TestBed.createComponent(HistoryTable);
    fixture.componentRef.setInput('page', PAGE);
    fixture.componentRef.setInput('sort', { column: 'date', direction: 'desc' });
    await fixture.whenStable();
  });

  it('renders one row per entry', () => {
    expect(element().querySelectorAll('tr[mat-row]').length).toBe(2);
  });

  it('shows the route as from → to', () => {
    expect(element().textContent).toContain('Haifa → Rotterdam');
  });

  it('shows a tag instead of a price for a failed reply', () => {
    expect(element().querySelector('.history-table__tag--failed')?.textContent).toContain('Failed');
  });

  it('shows response times in seconds', () => {
    expect(element().textContent).toContain('1.20 s');
  });

  it('reports the total count to the paginator', () => {
    expect(element().querySelector('.mat-mdc-paginator-range-label')?.textContent).toContain('42');
  });

  it('emits a one-based page when the paginator changes', () => {
    let paging: HistoryPaging | undefined;
    fixture.componentInstance.pageChange.subscribe((p) => (paging = p));

    fixture.componentInstance['onPage']({ pageIndex: 2, pageSize: 10, length: 42 });

    expect(paging).toEqual({ page: 3, pageSize: 10 });
  });

  it('emits a sort change for a known column', () => {
    let sort: HistorySort | undefined;
    fixture.componentInstance.sortChange.subscribe((s) => (sort = s));

    fixture.componentInstance['onSort']({ active: 'price', direction: 'asc' });

    expect(sort).toEqual({ column: 'price', direction: 'asc' });
  });

  it('ignores a sort change without a direction', () => {
    let sort: HistorySort | undefined;
    fixture.componentInstance.sortChange.subscribe((s) => (sort = s));

    fixture.componentInstance['onSort']({ active: 'price', direction: '' });

    expect(sort).toBeUndefined();
  });

  it('shows the empty message when the page has no rows', async () => {
    fixture.componentRef.setInput('page', { ...PAGE, items: [], totalCount: 0 });
    await fixture.whenStable();

    expect(element().textContent).toContain('No replies match these filters.');
  });

  function element(): HTMLElement {
    return fixture.nativeElement;
  }
});
