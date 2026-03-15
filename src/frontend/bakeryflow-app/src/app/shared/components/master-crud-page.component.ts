import { Component, Input, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { PageEvent } from '@angular/material/paginator';
import { OptionItem, PagedResult } from '../../core/models/api.models';
import { CrudResourceService } from '../../core/services/crud-resource.service';

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

  items: Record<string, unknown>[] = [];
  totalCount = 0;
  pageSize = 10;
  page = 1;
  search = '';
  loading = false;
  error = '';
  form!: FormGroup;
  editingId: string | null = null;
  options: Record<string, OptionItem[]> = {};
  displayedColumns: string[] = [];

  constructor(
    private readonly fb: FormBuilder,
    private readonly crudResourceService: CrudResourceService,
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
    this.error = '';
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
          this.loading = false;
        },
        error: (error: { error?: { message?: string } }) => {
          this.error = error.error?.message ?? 'No se pudo cargar la información.';
          this.loading = false;
        },
      });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    const payload = this.form.getRawValue();
    const request$ = this.editingId
      ? this.crudResourceService.update(this.endpoint, this.editingId, payload)
      : this.crudResourceService.create(this.endpoint, payload);

    request$.subscribe({
      next: () => {
        this.resetForm();
        this.loadItems();
      },
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'No se pudo guardar el registro.';
        this.loading = false;
      },
    });
  }

  edit(item: Record<string, unknown>): void {
    this.editingId = String(item['id']);
    this.form.patchValue(item);
  }

  toggleStatus(item: Record<string, unknown>): void {
    this.loading = true;
    this.crudResourceService.toggleStatus(this.endpoint, String(item['id'])).subscribe({
      next: () => this.loadItems(),
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'No se pudo cambiar el estado.';
        this.loading = false;
      },
    });
  }

  resetForm(): void {
    this.editingId = null;
    this.form.reset();
    this.fields.forEach((field) => {
      this.form.get(field.key)?.setValue(field.type === 'checkbox' ? true : '');
    });
    this.loading = false;
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
      return new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'USD' }).format(Number(value));
    }

    if (column.type === 'date') {
      return new Intl.DateTimeFormat('es-ES').format(new Date(String(value)));
    }

    if (column.type === 'boolean') {
      return value ? 'Activo' : 'Inactivo';
    }

    return String(value);
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
}
