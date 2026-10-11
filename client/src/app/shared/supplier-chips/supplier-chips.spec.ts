import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SupplierChips } from './supplier-chips';

const SUPPLIERS = [{ name: 'Oceanic' }, { name: 'BlueWave' }, { name: 'Nordic' }];

describe('SupplierChips', () => {
  let fixture: ComponentFixture<SupplierChips>;
  let emitted: readonly string[] | undefined;

  beforeEach(async () => {
    emitted = undefined;
    await TestBed.configureTestingModule({ imports: [SupplierChips] }).compileComponents();
    fixture = TestBed.createComponent(SupplierChips);
    fixture.componentRef.setInput('suppliers', SUPPLIERS);
    fixture.componentRef.setInput('selected', ['Oceanic', 'BlueWave']);
    fixture.componentInstance.selectedChange.subscribe((s) => (emitted = s));
    await fixture.whenStable();
  });

  it('shows how many suppliers are selected', () => {
    expect(element().textContent).toContain('2 of 3 selected');
  });

  it('marks the selected chips', () => {
    const selected = element().querySelectorAll('.mat-mdc-chip-selected');
    expect(selected.length).toBe(2);
  });

  it('emits every supplier on select all', () => {
    button('Select all').click();

    expect(emitted).toEqual(['Oceanic', 'BlueWave', 'Nordic']);
  });

  it('emits an empty list on clear', () => {
    button('Clear').click();

    expect(emitted).toEqual([]);
  });

  it('emits the selection without a chip that was deselected', () => {
    fixture.componentInstance['setSelected']('Oceanic', false);

    expect(emitted).toEqual(['BlueWave']);
  });

  it('does not emit when a chip is set to the value it already has', () => {
    fixture.componentInstance['setSelected']('Oceanic', true);

    expect(emitted).toBeUndefined();
  });

  function element(): HTMLElement {
    return fixture.nativeElement;
  }

  function button(label: string): HTMLButtonElement {
    const match = Array.from(element().querySelectorAll('button')).find((b) =>
      b.textContent?.includes(label),
    );
    if (!match) {
      throw new Error(`No button labelled ${label}`);
    }
    return match;
  }
});
