import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { SearchRequest } from '../../../core/api/search-events';
import { Supplier } from '../../../core/api/supplier';

/**
 * Presentational: the route, dates and supplier chips. Knows nothing about
 * the stream; it emits a validated SearchRequest and a cancel intent.
 */
@Component({
  selector: 'app-search-form',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatChipsModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './search-form.html',
  styleUrl: './search-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchForm {
  readonly suppliers = input.required<readonly Supplier[]>();
  readonly searching = input(false);

  readonly search = output<SearchRequest>();
  readonly cancel = output<void>();

  protected readonly form = new FormGroup({
    fromLocation: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    toLocation: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    fromDate: new FormControl<Date | null>(startOfToday(), [Validators.required]),
    toDate: new FormControl<Date | null>(daysFromToday(7), [Validators.required]),
  });

  /** Names deselected by the user; empty means every supplier, the default. */
  protected readonly deselected = signal<ReadonlySet<string>>(new Set());

  protected readonly selectedCount = computed(
    () => this.suppliers().filter((s) => !this.deselected().has(s.name)).length,
  );

  protected readonly canSearch = computed(() => this.selectedCount() > 0);

  protected isSelected(name: string): boolean {
    return !this.deselected().has(name);
  }

  /**
   * Material fires selectionChange for programmatic changes too, so the
   * state only moves when the chip's value actually differs.
   */
  protected setSelected(name: string, selected: boolean): void {
    if (this.isSelected(name) === selected) {
      return;
    }

    this.deselected.update((current) => {
      const next = new Set(current);
      if (selected) {
        next.delete(name);
      } else {
        next.add(name);
      }
      return next;
    });
  }

  protected selectAll(): void {
    this.deselected.set(new Set());
  }

  protected clearAll(): void {
    this.deselected.set(new Set(this.suppliers().map((s) => s.name)));
  }

  protected swap(): void {
    const { fromLocation, toLocation } = this.form.getRawValue();
    this.form.patchValue({ fromLocation: toLocation, toLocation: fromLocation });
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    const { fromLocation, toLocation, fromDate, toDate } = this.form.getRawValue();
    if (this.form.invalid || !this.canSearch() || !fromDate || !toDate) {
      return;
    }

    const selected = this.suppliers()
      .map((s) => s.name)
      .filter((name) => this.isSelected(name));

    this.search.emit({
      fromLocation: fromLocation.trim(),
      toLocation: toLocation.trim(),
      fromDate: toIsoDate(fromDate),
      toDate: toIsoDate(toDate),
      // Every supplier selected is the server's default, so send nothing.
      suppliers: selected.length === this.suppliers().length ? [] : selected,
    });
  }
}

function startOfToday(): Date {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return today;
}

function daysFromToday(days: number): Date {
  const date = startOfToday();
  date.setDate(date.getDate() + days);
  return date;
}

/** yyyy-MM-dd in local time, which is what the date pickers show. */
function toIsoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}
