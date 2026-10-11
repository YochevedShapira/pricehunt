import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RankedPrice } from '../search-state';
import { ResultsList } from './results-list';

const RESULTS: RankedPrice[] = [
  {
    rank: 1,
    supplierName: 'BlueWave',
    amount: 900,
    currency: 'USD',
    responseTimeMs: 1200,
    aboveCheapest: 0,
  },
  {
    rank: 2,
    supplierName: 'Oceanic',
    amount: 1500,
    currency: 'USD',
    responseTimeMs: 450,
    aboveCheapest: 600,
  },
];

describe('ResultsList', () => {
  let fixture: ComponentFixture<ResultsList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ResultsList] }).compileComponents();
    fixture = TestBed.createComponent(ResultsList);
  });

  it('renders the rows in the given order', async () => {
    fixture.componentRef.setInput('results', RESULTS);
    fixture.componentRef.setInput('hasSearched', true);
    await fixture.whenStable();

    expect(names()).toEqual(['BlueWave', 'Oceanic']);
  });

  it('labels the first row as cheapest and the others by their difference', async () => {
    fixture.componentRef.setInput('results', RESULTS);
    fixture.componentRef.setInput('hasSearched', true);
    await fixture.whenStable();

    expect(deltas()).toEqual(['Cheapest', '+$600.00']);
  });

  it('shows the no-results message after a search without prices', async () => {
    fixture.componentRef.setInput('results', []);
    fixture.componentRef.setInput('hasSearched', true);
    await fixture.whenStable();

    expect(element().textContent).toContain('No prices came back');
  });

  it('shows the welcome message before any search', async () => {
    fixture.componentRef.setInput('results', []);
    await fixture.whenStable();

    expect(element().textContent).toContain('Choose a route and dates');
  });

  function element(): HTMLElement {
    return fixture.nativeElement;
  }

  function names(): string[] {
    return Array.from(element().querySelectorAll('.results__name')).map(
      (n) => n.textContent?.trim() ?? '',
    );
  }

  function deltas(): string[] {
    return Array.from(element().querySelectorAll('.results__delta')).map(
      (n) => n.textContent?.trim() ?? '',
    );
  }
});
