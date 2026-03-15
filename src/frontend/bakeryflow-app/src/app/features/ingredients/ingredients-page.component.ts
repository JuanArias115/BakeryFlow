import { Component } from '@angular/core';
import { CrudColumnConfig, CrudFieldConfig } from '../../shared/components/master-crud-page.component';

@Component({
  selector: 'app-ingredients-page',
  template: `
    <app-master-crud-page
      title="Ingredientes y materias primas"
      description="Controla stock, costo promedio y unidad de medida."
      endpoint="ingredients"
      [columns]="columns"
      [fields]="fields"
    ></app-master-crud-page>
  `,
  standalone: false,
})
export class IngredientsPageComponent {
  readonly columns: CrudColumnConfig[] = [
    { key: 'code', label: 'Código' },
    { key: 'name', label: 'Nombre' },
    { key: 'unitName', label: 'Unidad' },
    { key: 'stockCurrent', label: 'Stock actual' },
    { key: 'averageCost', label: 'Costo promedio', type: 'currency' },
    { key: 'isLowStock', label: 'Stock bajo', type: 'boolean' },
  ];

  readonly fields: CrudFieldConfig[] = [
    { key: 'code', label: 'Código', type: 'text' },
    { key: 'name', label: 'Nombre', type: 'text', required: true },
    { key: 'unitOfMeasureId', label: 'Unidad de medida', type: 'select', required: true, endpoint: 'units' },
    { key: 'stockCurrent', label: 'Stock actual', type: 'number', required: true, step: '0.0001' },
    { key: 'stockMinimum', label: 'Stock mínimo', type: 'number', required: true, step: '0.0001' },
    { key: 'averageCost', label: 'Costo promedio', type: 'number', required: true, step: '0.0001' },
    { key: 'description', label: 'Descripción', type: 'textarea' },
    { key: 'isActive', label: 'Activo', type: 'checkbox' },
  ];
}
