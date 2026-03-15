import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-stat-card',
  template: `
    <article class="stat-card">
      <p class="label">{{ label }}</p>
      <h3>{{ value }}</h3>
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
}
