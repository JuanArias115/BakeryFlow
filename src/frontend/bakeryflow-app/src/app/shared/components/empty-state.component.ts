import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `
    <div class="empty-state">
      <div class="icon-wrap">
        <mat-icon>{{ icon }}</mat-icon>
      </div>
      <div class="copy">
        <h3>{{ title }}</h3>
        <p>{{ description }}</p>
      </div>
      <ng-content></ng-content>
    </div>
  `,
  styleUrl: './empty-state.component.scss',
  standalone: false,
})
export class EmptyStateComponent {
  @Input() icon = 'bakery_dining';
  @Input() title = 'Sin información disponible';
  @Input() description = 'Todavía no hay datos para mostrar en esta sección.';
}
