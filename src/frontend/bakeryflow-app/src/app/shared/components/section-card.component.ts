import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-section-card',
  template: `
    <section class="section-card">
      <header class="section-card__header">
        <div class="section-card__identity">
          <span class="section-card__icon" *ngIf="icon">
            <mat-icon>{{ icon }}</mat-icon>
          </span>
          <div>
            <p class="section-card__eyebrow" *ngIf="eyebrow">{{ eyebrow }}</p>
            <h3>{{ title }}</h3>
            <p class="section-card__subtitle" *ngIf="subtitle">{{ subtitle }}</p>
          </div>
        </div>
        <div class="section-card__actions">
          <ng-content select="[card-actions]"></ng-content>
        </div>
      </header>

      <div class="section-card__body">
        <ng-content></ng-content>
      </div>
    </section>
  `,
  styleUrl: './section-card.component.scss',
  standalone: false,
})
export class SectionCardComponent {
  @Input() title = '';
  @Input() subtitle = '';
  @Input() eyebrow = '';
  @Input() icon = '';
}
