import { Component, OnInit, TemplateRef, ViewChild, inject } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { finalize, forkJoin } from 'rxjs';
import { OptionItem, PagedResult } from '../../core/models/api.models';
import { ApiService } from '../../core/services/api.service';
import { formatCopCurrency } from '../../shared/utils/currency';

interface SaleItem {
  id: string;
  customerName: string | null;
  date: string;
  total: number;
  profit: number;
  paymentMethod: string;
}

interface ProductOption {
  id: string;
  name: string;
  salePrice: number;
}

@Component({
  selector: 'app-sales-page',
  templateUrl: './sales-page.component.html',
  styleUrl: './sales-page.component.scss',
  standalone: false,
})
export class SalesPageComponent implements OnInit {
  @ViewChild('saleDialogTemplate') saleDialogTemplate?: TemplateRef<unknown>;

  private readonly apiService = inject(ApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);

  items: SaleItem[] = [];
  customers: OptionItem[] = [];
  products: ProductOption[] = [];
  loading = true;
  submitting = false;
  error = '';
  paymentMethods = [
    { value: 1, label: 'Efectivo' },
    { value: 2, label: 'Tarjeta' },
    { value: 3, label: 'Transferencia' },
    { value: 4, label: 'Otro' },
  ];
  private dialogRef: MatDialogRef<unknown> | null = null;

  readonly form = this.formBuilder.group({
    customerId: [''],
    date: [this.currentDateTime(), Validators.required],
    paymentMethod: [1, Validators.required],
    notes: [''],
    details: this.formBuilder.array<FormGroup>([]),
  });

  get details(): FormArray<FormGroup> {
    return this.form.controls.details;
  }

  get total(): number {
    return this.details.controls.reduce((sum, group) => {
      const quantity = Number(group.get('quantity')?.value ?? 0);
      const unitPrice = Number(group.get('unitPrice')?.value ?? 0);
      return sum + quantity * unitPrice;
    }, 0);
  }

  ngOnInit(): void {
    this.loadReferenceData();
    this.loadSales();
  }

  openCreateDialog(): void {
    if (!this.saleDialogTemplate) {
      return;
    }

    this.error = '';
    this.resetForm();
    this.dialogRef = this.dialog.open(this.saleDialogTemplate, {
      width: 'min(960px, calc(100vw - 2rem))',
      maxWidth: 'calc(100vw - 2rem)',
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
        productId: ['', Validators.required],
        description: [''],
        quantity: [1, [Validators.required, Validators.min(0.01)]],
        unitPrice: [0, [Validators.required, Validators.min(1)]],
      }),
    );
  }

  removeLine(index: number): void {
    if (this.details.length === 1) {
      return;
    }

    this.details.removeAt(index);
  }

  onProductChange(index: number): void {
    const group = this.details.at(index);
    const product = this.products.find((item) => item.id === group.get('productId')?.value);
    if (!product) {
      return;
    }

    group.patchValue(
      {
        description: product.name,
        unitPrice: product.salePrice,
      },
      { emitEvent: false },
    );
  }

  submitSale(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.error = '';

    const payload = {
      ...this.form.getRawValue(),
      customerId: this.form.getRawValue().customerId || null,
    };

    this.apiService
      .post('sales', payload)
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: () => {
          this.closeDialog();
          this.loadSales();
        },
        error: (response) => {
          this.error = response?.error?.message ?? 'No fue posible registrar la venta.';
        },
      });
  }

  currency(value: number): string {
    return formatCopCurrency(value);
  }

  private loadReferenceData(): void {
    forkJoin({
      customers: this.apiService.getOptions('customers'),
      products: this.apiService.getPaged<{ id: string; name: string; salePrice: number }>('products', { page: 1, pageSize: 300 }),
    }).subscribe({
      next: ({ customers, products }) => {
        this.customers = customers;
        this.products = products.items.map((item) => ({
          id: item.id,
          name: item.name,
          salePrice: item.salePrice,
        }));
      },
      error: () => {
        this.error = 'No fue posible cargar clientes y productos.';
      },
    });
  }

  private loadSales(): void {
    this.loading = true;

    this.apiService
      .getPaged<SaleItem>('sales', { page: 1, pageSize: 30 })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (result: PagedResult<SaleItem>) => {
          this.items = result.items;
        },
        error: () => {
          this.error = 'No fue posible cargar las ventas.';
        },
      });
  }

  private resetForm(): void {
    this.form.reset({
      customerId: '',
      date: this.currentDateTime(),
      paymentMethod: 1,
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
}
