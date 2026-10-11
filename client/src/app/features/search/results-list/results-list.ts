import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RankedPrice } from '../search-state';

/**
 * Presentational: the prices, cheapest first. Rows are tracked by supplier
 * name, so when a new price arrives and the order changes Angular moves the
 * existing DOM nodes instead of recreating them: no flicker.
 */
@Component({
  selector: 'app-results-list',
  imports: [CurrencyPipe],
  templateUrl: './results-list.html',
  styleUrl: './results-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResultsList {
  readonly results = input.required<readonly RankedPrice[]>();
  readonly route = input('');
  /** True once a search has run: switches the empty state's message. */
  readonly hasSearched = input(false);
  readonly searching = input(false);

  protected seconds(ms: number): string {
    return `${(ms / 1000).toFixed(2)} s`;
  }
}
