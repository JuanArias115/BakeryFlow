import { Component } from '@angular/core';
import { CrudColumnConfig, CrudFieldConfig } from '../../shared/components/master-crud-page.component';

@Component({
  selector: 'app-units-page',
  template: `
    <app-master-crud-page
      title="Unidades de medida"
      description="Define las unidades usadas en ingredientes, compras y recetas."
      icon="straighten"
      endpoint="units"
      [columns]="columns"
      [fields]="fields"
    ></app-master-crud-page>
  `,
  standalone: false,
})
export class UnitsPageComponent {
  readonly columns: CrudColumnConfig[] = [
    { key: 'name', label: 'Nombre' },
    { key: 'abbreviation', label: 'Abreviatura' },
    { key: 'type', label: 'Tipo' },
    { key: 'isActive', label: 'Estado', type: 'boolean' },
  ];

  readonly fields: CrudFieldConfig[] = [
    { key: 'name', label: 'Nombre', type: 'text', required: true },
    { key: 'abbreviation', label: 'Abreviatura', type: 'text', required: true },
    { key: 'type', label: 'Tipo', type: 'text' },
    { key: 'isActive', label: 'Activo', type: 'checkbox' },
  ];
}
