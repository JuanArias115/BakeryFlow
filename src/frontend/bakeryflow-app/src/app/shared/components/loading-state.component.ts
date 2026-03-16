import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-loading-state',
  template: `
    <div class="loading-shell" [class.compact]="compact">
      <div class="loading-copy" *ngIf="title || description">
        <strong *ngIf="title">{{ title }}</strong>
        <p *ngIf="description">{{ description }}</p>
      </div>

      <div class="skeleton-list">
        <span *ngFor="let row of rowsArray" class="skeleton-row"></span>
      </div>
    </div>
  `,
  styleUrl: './loading-state.component.scss',
  standalone: false,
})
export class LoadingStateComponent {
  @Input() title = 'Cargando información';
  @Input() description = 'Estamos preparando la vista.';
  @Input() rows = 4;
  @Input() compact = false;

  get rowsArray(): number[] {
    return Array.from({ length: this.rows }, (_, index) => index);
  }
}
