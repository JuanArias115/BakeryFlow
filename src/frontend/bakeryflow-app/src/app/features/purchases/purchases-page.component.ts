import { Component, OnInit, TemplateRef, ViewChild, inject } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { finalize, forkJoin } from 'rxjs';
import { OptionItem, PagedResult } from '../../core/models/api.models';
import { ApiService } from '../../core/services/api.service';
import { formatCopCurrency } from '../../shared/utils/currency';

interface PurchaseItem {
  id: string;
  supplierId: string;
  supplierName: string;
  invoiceNumber: string | null;
  purchaseDate: string;
  total: number;
  status: string;
}

interface IngredientOption {
  id: string;
  name: string;
  unitOfMeasureId: string;
  unitName: string;
  averageCost: number;
}

@Component({
  selector: 'app-purchases-page',
  templateUrl: './purchases-page.component.html',
  styleUrl: './purchases-page.component.scss',
  standalone: false,
})
export class PurchasesPageComponent implements OnInit {
  @ViewChild('purchaseDialogTemplate') purchaseDialogTemplate?: TemplateRef<unknown>;

  private readonly apiService = inject(ApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);

  items: PurchaseItem[] = [];
  suppliers: OptionItem[] = [];
  ingredients: IngredientOption[] = [];
  loading = true;
  loadingReferences = true;
  submitting = false;
  error = '';
  private dialogRef: MatDialogRef<unknown> | null = null;

  readonly form = this.formBuilder.group({
    supplierId: ['', Validators.required],
    invoiceNumber: [''],
    purchaseDate: [this.currentDateTime(), Validators.required],
    notes: [''],
    details: this.formBuilder.array<FormGroup>([]),
  });

  get details(): FormArray<FormGroup> {
    return this.form.controls.details;
  }

  get total(): number {
    return this.details.controls.reduce((sum, group) => {
      const quantity = Number(group.get('quantity')?.value ?? 0);
      const unitCost = Number(group.get('unitCost')?.value ?? 0);
      return sum + quantity * unitCost;
    }, 0);
  }

  ngOnInit(): void {
    this.loadReferenceData();
    this.loadPurchases();
  }

  openCreateDialog(): void {
    if (!this.purchaseDialogTemplate) {
      return;
    }

    this.error = '';
    this.resetForm();
    this.dialogRef = this.dialog.open(this.purchaseDialogTemplate, {
      width: 'min(1120px, calc(100vw - 2rem))',
      maxWidth: 'calc(100vw - 2rem)',
      maxHeight: 'calc(100vh - 2rem)',
      panelClass: 'bf-dialog-panel',
      autoFocus: false,
    });

    this.dialogRef.afterClosed().subscribe(() => {
      this.dialogRef = null;
    });
  }

  closeDialog(): void {
    this.dialogRef?.close();
    this.dialogRef = null;
  }

  addLine(): void {
    this.details.push(
      this.formBuilder.group({
        ingredientId: ['', Validators.required],
        description: ['', Validators.required],
        quantity: [1, [Validators.required, Validators.min(0.01)]],
        unitOfMeasureId: ['', Validators.required],
        unitName: [''],
        packageQuantity: [0, [Validators.min(0)]],
        packageCost: [0, [Validators.min(0)]],
        unitCost: [0, [Validators.required, Validators.min(0)]],
      }),
    );
  }

  removeLine(index: number): void {
    if (this.details.length === 1) {
      return;
    }

    this.details.removeAt(index);
  }

  onIngredientChange(index: number): void {
    const group = this.details.at(index);
    const ingredient = this.ingredients.find((item) => item.id === group.get('ingredientId')?.value);
    if (!ingredient) {
      return;
    }

    group.patchValue(
      {
        description: ingredient.name,
        unitOfMeasureId: ingredient.unitOfMeasureId,
        unitName: ingredient.unitName,
        packageQuantity: Number(group.get('quantity')?.value ?? 0),
        packageCost: 0,
        unitCost: ingredient.averageCost || 0,
      },
      { emitEvent: false },
    );
  }

  recalculateUnitCost(index: number): void {
    const group = this.details.at(index);
    const packageQuantity = Number(group.get('packageQuantity')?.value ?? 0);
    const packageCost = Number(group.get('packageCost')?.value ?? 0);
    if (packageQuantity <= 0 || packageCost < 0) {
      return;
    }

    group.patchValue(
      {
        unitCost: this.roundValue(packageCost / packageQuantity),
      },
      { emitEvent: false },
    );
  }

  submitPurchase(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.error = '';

    const payload = {
      ...this.form.getRawValue(),
      details: this.details.controls.map((group) => ({
        ingredientId: group.get('ingredientId')?.value,
        description: group.get('description')?.value,
        quantity: Number(group.get('quantity')?.value ?? 0),
        unitOfMeasureId: group.get('unitOfMeasureId')?.value,
        unitCost: Number(group.get('unitCost')?.value ?? 0),
      })),
    };

    this.apiService
      .post('purchases', payload)
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: () => {
          this.closeDialog();
          this.loadPurchases();
        },
        error: (response) => {
          this.error = response?.error?.message ?? 'No fue posible registrar la compra.';
        },
      });
  }

  confirmPurchase(id: string): void {
    this.submitting = true;
    this.error = '';

    this.apiService
      .post(`purchases/${id}/confirm`, {})
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: () => this.loadPurchases(),
        error: (response) => {
          this.error = response?.error?.message ?? 'No fue posible confirmar la compra.';
        },
      });
  }

  canConfirm(status: string): boolean {
    return status.toLowerCase() === 'draft';
  }

  currency(value: number): string {
    return formatCopCurrency(value);
  }

  unitLabel(index: number): string {
    return this.details.at(index).get('unitName')?.value || 'unidad base';
  }

  lineSubtotal(index: number): number {
    const group = this.details.at(index);
    const quantity = Number(group.get('quantity')?.value ?? 0);
    const unitCost = Number(group.get('unitCost')?.value ?? 0);
    return quantity * unitCost;
  }

  private loadReferenceData(): void {
    this.loadingReferences = true;
    forkJoin({
      suppliers: this.apiService.getOptions('suppliers'),
      ingredients: this.apiService.getPaged<{
        id: string;
        name: string;
        unitOfMeasureId: string;
        unitName: string;
        averageCost: number;
      }>('ingredients', { page: 1, pageSize: 100 }),
    }).subscribe({
      next: ({ suppliers, ingredients }) => {
        this.suppliers = suppliers;
        this.ingredients = ingredients.items.map((item) => ({
          id: item.id,
          name: item.name,
          unitOfMeasureId: item.unitOfMeasureId,
          unitName: item.unitName,
          averageCost: item.averageCost,
        }));
        this.loadingReferences = false;
      },
      error: () => {
        this.error = 'No fue posible cargar proveedores e ingredientes.';
        this.loadingReferences = false;
      },
    });
  }

  private loadPurchases(): void {
    this.loading = true;

    this.apiService
      .getPaged<PurchaseItem>('purchases', { page: 1, pageSize: 30 })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (result: PagedResult<PurchaseItem>) => {
          this.items = result.items;
        },
        error: () => {
          this.error = 'No fue posible cargar las compras.';
        },
      });
  }

  private resetForm(): void {
    this.form.reset({
      supplierId: '',
      invoiceNumber: '',
      purchaseDate: this.currentDateTime(),
      notes: '',
    });

    this.details.clear();
    this.addLine();
  }

  private currentDateTime(): string {
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    return now.toISOString().slice(0, 16);
  }

  private roundValue(value: number): number {
    return Math.round(value * 10000) / 10000;
  }
}
