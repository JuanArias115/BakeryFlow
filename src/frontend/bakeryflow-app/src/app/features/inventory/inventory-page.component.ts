import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { ApiService } from '../../core/services/api.service';
import { OptionItem, PagedResult } from '../../core/models/api.models';
import { formatCopCurrency } from '../../shared/utils/currency';

interface InventoryStock {
  ingredientId: string;
  ingredientName: string;
  unitName: string;
  stockCurrent: number;
  stockMinimum: number;
  averageCost: number;
  isLowStock: boolean;
}

interface InventoryMovement {
  id: string;
  ingredientName: string;
  type: string;
  date: string;
  quantityIn: number;
  quantityOut: number;
  resultingBalance: number;
}

@Component({
  selector: 'app-inventory-page',
  templateUrl: './inventory-page.component.html',
  styleUrl: './inventory-page.component.scss',
  standalone: false,
})
export class InventoryPageComponent implements OnInit {
  @ViewChild('adjustmentDialogTemplate') adjustmentDialogTemplate?: TemplateRef<unknown>;
  stocks: InventoryStock[] = [];
  movements: InventoryMovement[] = [];
  ingredients: OptionItem[] = [];
  loading = true;
  error = '';
  readonly form;
  private dialogRef: MatDialogRef<unknown> | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly apiService: ApiService,
    private readonly dialog: MatDialog,
  ) {
    this.form = this.fb.group({
      ingredientId: ['', Validators.required],
      quantityDelta: [0, Validators.required],
      unitCost: [0],
      notes: [''],
      date: [''],
    });
  }

  ngOnInit(): void {
    this.loadStocks();
    this.loadMovements();
    this.apiService.getOptions('ingredients').subscribe((items) => {
      this.ingredients = items;
    });
  }

  loadStocks(): void {
    this.loading = true;
    this.apiService.getPaged<InventoryStock>('inventory/stocks', { page: 1, pageSize: 30 }).subscribe({
      next: (result: PagedResult<InventoryStock>) => {
        this.stocks = result.items;
        this.error = '';
        this.loading = false;
      },
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'No se pudieron cargar las existencias.';
        this.loading = false;
      },
    });
  }

  loadMovements(): void {
    this.apiService.getPaged<InventoryMovement>('inventory/movements', { page: 1, pageSize: 20 }).subscribe((result: PagedResult<InventoryMovement>) => {
      this.movements = result.items;
    });
  }

  submitAdjustment(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.apiService.post('inventory/adjustments', value).subscribe(() => {
      this.closeAdjustmentDialog();
      this.loadStocks();
      this.loadMovements();
    });
  }

  openAdjustmentDialog(): void {
    this.form.reset({ ingredientId: '', quantityDelta: 0, unitCost: 0, notes: '', date: '' });
    if (!this.adjustmentDialogTemplate) {
      return;
    }

    this.dialogRef = this.dialog.open(this.adjustmentDialogTemplate, {
      width: 'min(760px, calc(100vw - 2rem))',
      maxWidth: 'calc(100vw - 2rem)',
      panelClass: 'bf-dialog-panel',
      autoFocus: false,
    });

    this.dialogRef.afterClosed().subscribe(() => {
      this.dialogRef = null;
      this.form.reset({ ingredientId: '', quantityDelta: 0, unitCost: 0, notes: '', date: '' });
    });
  }

  closeAdjustmentDialog(): void {
    this.dialogRef?.close();
    this.dialogRef = null;
    this.form.reset({ ingredientId: '', quantityDelta: 0, unitCost: 0, notes: '', date: '' });
  }

  currency(value: number): string {
    return formatCopCurrency(value);
  }
}
