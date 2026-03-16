import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-stat-card',
  template: `
    <article class="stat-card">
      <div class="stat-card__head">
        <div>
          <p class="label">{{ label }}</p>
          <h3>{{ value }}</h3>
        </div>
        <span class="stat-card__icon" *ngIf="icon">
          <mat-icon>{{ icon }}</mat-icon>
        </span>
      </div>
      <p class="hint" *ngIf="hint">{{ hint }}</p>
    </article>
  `,
  styleUrl: './stat-card.component.scss',
  standalone: false,
})
export class StatCardComponent {
  @Input() label = '';
  @Input() value = '';
  @Input() hint = '';
  @Input() icon = '';
}
