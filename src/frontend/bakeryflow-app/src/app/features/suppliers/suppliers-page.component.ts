import { Component } from '@angular/core';
import { CrudColumnConfig, CrudFieldConfig } from '../../shared/components/master-crud-page.component';

@Component({
  selector: 'app-suppliers-page',
  template: `
    <app-master-crud-page
      title="Proveedores"
      description="Mantén tu base de proveedores y contactos de compra."
      icon="local_shipping"
      endpoint="suppliers"
      [columns]="columns"
      [fields]="fields"
    ></app-master-crud-page>
  `,
  standalone: false,
})
export class SuppliersPageComponent {
  readonly columns: CrudColumnConfig[] = [
    { key: 'name', label: 'Nombre' },
    { key: 'phone', label: 'Teléfono' },
    { key: 'email', label: 'Email' },
    { key: 'contact', label: 'Contacto' },
    { key: 'isActive', label: 'Estado', type: 'boolean' },
  ];

  readonly fields: CrudFieldConfig[] = [
    { key: 'name', label: 'Nombre', type: 'text', required: true },
    { key: 'phone', label: 'Teléfono', type: 'text' },
    { key: 'email', label: 'Email', type: 'email' },
    { key: 'address', label: 'Dirección', type: 'textarea' },
    { key: 'contact', label: 'Contacto', type: 'text' },
    { key: 'notes', label: 'Observaciones', type: 'textarea' },
    { key: 'isActive', label: 'Activo', type: 'checkbox' },
  ];
}
