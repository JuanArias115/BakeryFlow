import { Component } from '@angular/core';
import { CrudColumnConfig, CrudFieldConfig } from '../../shared/components/master-crud-page.component';

@Component({
  selector: 'app-products-page',
  template: `
    <app-master-crud-page
      title="Productos"
      description="Define el catálogo comercial con precio de venta y categoría."
      endpoint="products"
      [columns]="columns"
      [fields]="fields"
    ></app-master-crud-page>
  `,
  standalone: false,
})
export class ProductsPageComponent {
  readonly columns: CrudColumnConfig[] = [
    { key: 'code', label: 'Código' },
    { key: 'name', label: 'Nombre' },
    { key: 'categoryName', label: 'Categoría' },
    { key: 'unitSale', label: 'Unidad venta' },
    { key: 'salePrice', label: 'Precio', type: 'currency' },
    { key: 'isActive', label: 'Estado', type: 'boolean' },
  ];

  readonly fields: CrudFieldConfig[] = [
    { key: 'code', label: 'Código', type: 'text' },
    { key: 'name', label: 'Nombre', type: 'text', required: true },
    { key: 'categoryId', label: 'Categoría', type: 'select', required: true, endpoint: 'categories' },
    { key: 'unitSale', label: 'Unidad de venta', type: 'text', required: true },
    { key: 'salePrice', label: 'Precio de venta', type: 'number', required: true, step: '0.01' },
    { key: 'description', label: 'Descripción', type: 'textarea' },
    { key: 'isActive', label: 'Activo', type: 'checkbox' },
  ];
}
