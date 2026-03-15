import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">{{ eyebrow }}</p>
        <h1>{{ title }}</h1>
        <p class="description" *ngIf="description">{{ description }}</p>
      </div>
      <ng-content></ng-content>
    </div>
  `,
  styleUrl: './page-header.component.scss',
  standalone: false,
})
export class PageHeaderComponent {
  @Input() eyebrow = 'BakeryFlow';
  @Input() title = '';
  @Input() description = '';
}
