import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { Supplier } from '../../core/api/supplier';

/**
 * Presentational, fully controlled: a chip per supplier with select all and
 * clear. The parent owns the selection and receives every change.
 */
@Component({
  selector: 'app-supplier-chips',
  imports: [MatButtonModule, MatChipsModule],
  templateUrl: './supplier-chips.html',
  styleUrl: './supplier-chips.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupplierChips {
  readonly suppliers = input.required<readonly Supplier[]>();
  readonly selected = input.required<readonly string[]>();
  readonly label = input('Suppliers');

  readonly selectedChange = output<readonly string[]>();

  protected readonly selectedSet = computed(() => new Set(this.selected()));

  protected isSelected(name: string): boolean {
    return this.selectedSet().has(name);
  }

  /**
   * Only user clicks reach this (the template checks isUserInput), and a
   * click that leaves the value unchanged emits nothing.
   */
  protected setSelected(name: string, selected: boolean): void {
    if (this.isSelected(name) === selected) {
      return;
    }

    this.selectedChange.emit(
      this.suppliers()
        .map((s) => s.name)
        .filter((n) => (n === name ? selected : this.isSelected(n))),
    );
  }

  protected selectAll(): void {
    this.selectedChange.emit(this.suppliers().map((s) => s.name));
  }

  protected clearAll(): void {
    this.selectedChange.emit([]);
  }
}
