import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  output,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { debounceTime } from 'rxjs';
import { HistoryFilters } from '../../../core/api/history';
import { Supplier } from '../../../core/api/supplier';
import { toIsoDate } from '../../../shared/iso-date';
import { SupplierChips } from '../../../shared/supplier-chips/supplier-chips';

/**
 * Presentational: date range, locations and supplier chips for the history
 * table. Emits the complete filter set whenever any part changes; typing
 * is debounced so a request is not sent per keystroke.
 */
@Component({
  selector: 'app-history-filters',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    SupplierChips,
  ],
  templateUrl: './history-filters.html',
  styleUrl: './history-filters.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoryFiltersPanel {
  private static readonly TYPING_DEBOUNCE_MS = 300;

  readonly suppliers = input.required<readonly Supplier[]>();
  /** The current selection; the parent owns it. */
  readonly selectedSuppliers = input.required<readonly string[]>();

  readonly filtersChange = output<HistoryFilters>();
  readonly reset = output<void>();

  protected readonly form = new FormGroup({
    startDate: new FormControl<Date | null>(null),
    endDate: new FormControl<Date | null>(null),
    fromLocation: new FormControl('', { nonNullable: true }),
    toLocation: new FormControl('', { nonNullable: true }),
  });

  constructor() {
    this.form.valueChanges
      .pipe(
        debounceTime(HistoryFiltersPanel.TYPING_DEBOUNCE_MS),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe(() => this.emit(this.selectedSuppliers()));
  }

  protected onSelectedChange(selected: readonly string[]): void {
    this.emit(selected);
  }

  protected resetAll(): void {
    this.form.reset(
      { startDate: null, endDate: null, fromLocation: '', toLocation: '' },
      { emitEvent: false },
    );
    this.reset.emit();
  }

  private emit(suppliers: readonly string[]): void {
    const { startDate, endDate, fromLocation, toLocation } = this.form.getRawValue();
    this.filtersChange.emit({
      startDate: startDate ? toIsoDate(startDate) : null,
      endDate: endDate ? toIsoDate(endDate) : null,
      suppliers,
      fromLocation,
      toLocation,
    });
  }
}
