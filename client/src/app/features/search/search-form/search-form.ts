import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { SearchRequest } from '../../../core/api/search-events';
import { Supplier } from '../../../core/api/supplier';
import { toIsoDate } from '../../../shared/iso-date';
import { SupplierChips } from '../../../shared/supplier-chips/supplier-chips';

/**
 * Presentational: the route, dates and supplier chips. Knows nothing about
 * the stream; it emits a validated SearchRequest and a cancel intent.
 */
@Component({
  selector: 'app-search-form',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    SupplierChips,
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

  /**
   * Names the user deselected. Stored this way round so that "everything
   * selected", the brief's default, holds before the catalog has loaded.
   */
  private readonly deselected = signal<ReadonlySet<string>>(new Set());

  protected readonly selected = computed(() =>
    this.suppliers()
      .map((s) => s.name)
      .filter((name) => !this.deselected().has(name)),
  );

  protected readonly canSearch = computed(() => this.selected().length > 0);

  protected onSelectedChange(selected: readonly string[]): void {
    const chosen = new Set(selected);
    this.deselected.set(
      new Set(
        this.suppliers()
          .map((s) => s.name)
          .filter((name) => !chosen.has(name)),
      ),
    );
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

    const selected = this.selected();
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
