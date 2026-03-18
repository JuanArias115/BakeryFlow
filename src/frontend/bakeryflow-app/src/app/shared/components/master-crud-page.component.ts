import { Component, Input, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { PageEvent } from '@angular/material/paginator';
import { OptionItem, PagedResult } from '../../core/models/api.models';
import { CrudResourceService } from '../../core/services/crud-resource.service';
import { formatCopCurrency } from '../utils/currency';

export interface CrudColumnConfig {
  key: string;
  label: string;
  type?: 'text' | 'currency' | 'date' | 'boolean';
}

export interface CrudFieldConfig {
  key: string;
  label: string;
  type: 'text' | 'textarea' | 'number' | 'email' | 'checkbox' | 'select';
  required?: boolean;
  endpoint?: string;
  step?: string;
}

@Component({
  selector: 'app-master-crud-page',
  templateUrl: './master-crud-page.component.html',
  styleUrl: './master-crud-page.component.scss',
  standalone: false,
})
export class MasterCrudPageComponent implements OnInit {
  @Input({ required: true }) title = '';
  @Input() description = '';
  @Input({ required: true }) endpoint = '';
  @Input({ required: true }) columns: CrudColumnConfig[] = [];
  @Input({ required: true }) fields: CrudFieldConfig[] = [];
  @Input() icon = 'inventory_2';

  items: Record<string, unknown>[] = [];
  totalCount = 0;
  pageSize = 10;
  page = 1;
  search = '';
  loading = true;
  submitting = false;
  error = '';
  form!: FormGroup;
  editingId: string | null = null;
  options: Record<string, OptionItem[]> = {};
  displayedColumns: string[] = [];
  @ViewChild('formDialogTemplate') formDialogTemplate?: TemplateRef<unknown>;
  private dialogRef: MatDialogRef<unknown> | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly crudResourceService: CrudResourceService,
    private readonly dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.displayedColumns = [...this.columns.map((column) => column.key), 'actions'];
    this.form = this.fb.group(
      this.fields.reduce<Record<string, FormControl>>((accumulator, field) => {
        accumulator[field.key] = new FormControl(
          field.type === 'checkbox' ? true : '',
          field.required ? Validators.required : [],
        );
        return accumulator;
      }, {}),
    );

    this.loadOptions();
    this.loadItems();
  }

  loadItems(): void {
    this.loading = true;
    this.crudResourceService
      .getPaged<Record<string, unknown>>(this.endpoint, {
        page: this.page,
        pageSize: this.pageSize,
        search: this.search,
      })
      .subscribe({
        next: (result: PagedResult<Record<string, unknown>>) => {
          this.items = result.items;
          this.totalCount = result.totalCount;
          this.error = '';
          this.loading = false;
          this.submitting = false;
        },
        error: (error: { error?: { message?: string } }) => {
          this.error = error.error?.message ?? 'No se pudo cargar la información.';
          this.items = [];
          this.loading = false;
          this.submitting = false;
        },
      });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.error = '';

    const payload = this.form.getRawValue();
    const request$ = this.editingId
      ? this.crudResourceService.update(this.endpoint, this.editingId, payload)
      : this.crudResourceService.create(this.endpoint, payload);

    request$.subscribe({
      next: () => {
        this.closeDialog();
        this.resetForm();
        this.loadItems();
      },
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'No se pudo guardar el registro.';
        this.submitting = false;
      },
    });
  }

  edit(item: Record<string, unknown>): void {
    this.editingId = String(item['id']);
    this.form.patchValue(item);
    this.openDialog();
  }

  toggleStatus(item: Record<string, unknown>): void {
    this.submitting = true;
    this.error = '';
    this.crudResourceService.toggleStatus(this.endpoint, String(item['id'])).subscribe({
      next: () => this.loadItems(),
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'No se pudo cambiar el estado.';
        this.submitting = false;
      },
    });
  }

  resetForm(): void {
    this.editingId = null;
    this.form.reset();
    this.fields.forEach((field) => {
      this.form.get(field.key)?.setValue(field.type === 'checkbox' ? true : '');
    });
    this.submitting = false;
  }

  openCreateDialog(): void {
    this.resetForm();
    this.openDialog();
  }

  closeFormDialog(): void {
    this.closeDialog();
    this.resetForm();
  }

  onSearchChange(value: string): void {
    this.search = value;
    this.page = 1;
    this.loadItems();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadItems();
  }

  renderValue(item: Record<string, unknown>, column: CrudColumnConfig): string {
    const value = item[column.key];
    if (value === null || value === undefined || value === '') {
      return '—';
    }

    if (column.type === 'currency') {
      return formatCopCurrency(Number(value));
    }

    if (column.type === 'date') {
      return new Intl.DateTimeFormat('es-ES').format(new Date(String(value)));
    }

    if (column.type === 'boolean') {
      return value ? 'Activo' : 'Inactivo';
    }

    return String(value);
  }

  get isEmpty(): boolean {
    return !this.loading && !this.error && this.items.length === 0;
  }

  trackByKey(_index: number, field: CrudFieldConfig): string {
    return field.key;
  }

  fieldError(fieldKey: string): string {
    const control = this.form.get(fieldKey);
    if (!control || !control.touched || !control.errors) {
      return '';
    }

    if (control.errors['required']) {
      return 'Este campo es obligatorio.';
    }

    if (control.errors['email']) {
      return 'Ingresa un correo válido.';
    }

    return 'Revisa este dato.';
  }

  private loadOptions(): void {
    this.fields
      .filter((field) => field.type === 'select' && field.endpoint)
      .forEach((field) => {
        this.crudResourceService.getOptions(field.endpoint!).subscribe({
          next: (options) => {
            this.options[field.key] = options;
          },
        });
      });
  }

  private openDialog(): void {
    if (!this.formDialogTemplate) {
      return;
    }

    this.dialogRef = this.dialog.open(this.formDialogTemplate, {
      width: 'min(720px, calc(100vw - 2rem))',
      maxWidth: 'calc(100vw - 2rem)',
      panelClass: 'bf-dialog-panel',
      autoFocus: false,
    });
    this.dialogRef.afterClosed().subscribe(() => {
      this.dialogRef = null;
      this.resetForm();
    });
  }

  private closeDialog(): void {
    this.dialogRef?.close();
    this.dialogRef = null;
  }
}
