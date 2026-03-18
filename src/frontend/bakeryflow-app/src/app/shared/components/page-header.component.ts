import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  template: `
    <div class="page-header">
      <div class="page-header__identity">
        <span class="page-header__icon" *ngIf="icon">
          <mat-icon>{{ icon }}</mat-icon>
        </span>
        <div class="page-header__copy">
          <p class="eyebrow">{{ eyebrow }}</p>
          <h1>{{ title }}</h1>
          <p class="description" *ngIf="description">{{ description }}</p>
        </div>
      </div>
      <div class="page-header__actions">
        <ng-content></ng-content>
      </div>
    </div>
  `,
  styleUrl: './page-header.component.scss',
  standalone: false,
})
export class PageHeaderComponent {
  @Input() eyebrow = 'BakeryFlow';
  @Input() title = '';
  @Input() description = '';
  @Input() icon = 'bakery_dining';
}
