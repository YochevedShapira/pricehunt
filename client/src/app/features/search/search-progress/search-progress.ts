import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { SearchPhase, SearchProgress, SupplierStatus } from '../search-state';

interface Badge {
  readonly label: string;
  readonly tone: 'searching' | 'completed' | 'timedout' | 'cancelled' | 'error';
}

const BADGES: Record<Exclude<SearchPhase, 'idle'>, Badge> = {
  searching: { label: 'Searching', tone: 'searching' },
  Completed: { label: 'Completed', tone: 'completed' },
  TimedOut: { label: 'Timed out', tone: 'timedout' },
  Cancelled: { label: 'Cancelled', tone: 'cancelled' },
  error: { label: 'Error', tone: 'error' },
};

/**
 * Presentational: status badge, "x of n responded", progress bar, a summary
 * sentence and the per-supplier status grid.
 */
@Component({
  selector: 'app-search-progress',
  imports: [CurrencyPipe, MatProgressBarModule],
  templateUrl: './search-progress.html',
  styleUrl: './search-progress.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchProgressPanel {
  readonly phase = input.required<Exclude<SearchPhase, 'idle'>>();
  readonly progress = input.required<SearchProgress>();
  readonly suppliers = input.required<readonly SupplierStatus[]>();
  readonly searchNo = input.required<number>();
  readonly elapsedMs = input.required<number>();
  readonly replacedRunningSearch = input(false);
  readonly error = input<string | undefined>(undefined);

  protected readonly badge = computed(() => BADGES[this.phase()]);

  protected readonly summary = computed(() =>
    summarise(this.phase(), this.suppliers(), this.elapsedMs()),
  );

  protected readonly elapsedSeconds = computed(() => (this.elapsedMs() / 1000).toFixed(1));
}

function summarise(
  phase: Exclude<SearchPhase, 'idle'>,
  suppliers: readonly SupplierStatus[],
  elapsedMs: number,
): string {
  const priced = suppliers.filter((s) => s.outcome === 'priced').length;
  const failed = suppliers.filter((s) => s.outcome === 'failed').length;
  const silent = suppliers.filter((s) => s.outcome === 'noResponse').length;
  const prices = plural(priced, 'price');
  const errors = failed ? ` ${failed} returned an error.` : '';

  switch (phase) {
    case 'searching':
      return '';
    case 'Completed':
      return `All ${suppliers.length} suppliers answered in ${(elapsedMs / 1000).toFixed(1)} s.${errors}`;
    case 'TimedOut':
      return `Stopped at the time limit with ${prices}. ${plural(silent, 'supplier')} did not respond.${errors}`;
    case 'Cancelled':
      return `Search cancelled. The ${prices} collected so far are kept.`;
    case 'error':
      return `The connection to the server was lost. The ${prices} collected so far are kept.`;
  }
}

function plural(count: number, noun: string): string {
  return `${count} ${noun}${count === 1 ? '' : 's'}`;
}
