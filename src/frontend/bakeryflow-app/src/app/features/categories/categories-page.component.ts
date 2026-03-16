import { Component } from '@angular/core';
import { CrudColumnConfig, CrudFieldConfig } from '../../shared/components/master-crud-page.component';

@Component({
  selector: 'app-categories-page',
  template: `
    <app-master-crud-page
      title="Categorías"
      description="Organiza los productos por familias y controla su estado."
      icon="category"
      endpoint="categories"
      [columns]="columns"
      [fields]="fields"
    ></app-master-crud-page>
  `,
  standalone: false,
})
export class CategoriesPageComponent {
  readonly columns: CrudColumnConfig[] = [
    { key: 'name', label: 'Nombre' },
    { key: 'description', label: 'Descripción' },
    { key: 'isActive', label: 'Estado', type: 'boolean' },
    { key: 'updatedAt', label: 'Actualizado', type: 'date' },
  ];

  readonly fields: CrudFieldConfig[] = [
    { key: 'name', label: 'Nombre', type: 'text', required: true },
    { key: 'description', label: 'Descripción', type: 'textarea' },
    { key: 'isActive', label: 'Activo', type: 'checkbox' },
  ];
}
