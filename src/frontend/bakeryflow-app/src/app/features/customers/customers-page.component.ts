import { Component } from '@angular/core';
import { CrudColumnConfig, CrudFieldConfig } from '../../shared/components/master-crud-page.component';

@Component({
  selector: 'app-customers-page',
  template: `
    <app-master-crud-page
      title="Clientes"
      description="Administra los clientes frecuentes y sus datos de contacto."
      endpoint="customers"
      [columns]="columns"
      [fields]="fields"
    ></app-master-crud-page>
  `,
  standalone: false,
})
export class CustomersPageComponent {
  readonly columns: CrudColumnConfig[] = [
    { key: 'name', label: 'Nombre' },
    { key: 'phone', label: 'Teléfono' },
    { key: 'email', label: 'Email' },
    { key: 'isActive', label: 'Estado', type: 'boolean' },
  ];

  readonly fields: CrudFieldConfig[] = [
    { key: 'name', label: 'Nombre', type: 'text', required: true },
    { key: 'phone', label: 'Teléfono', type: 'text' },
    { key: 'email', label: 'Email', type: 'email' },
    { key: 'address', label: 'Dirección', type: 'textarea' },
    { key: 'notes', label: 'Observaciones', type: 'textarea' },
    { key: 'isActive', label: 'Activo', type: 'checkbox' },
  ];
}
