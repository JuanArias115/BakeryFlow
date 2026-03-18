import { Component } from '@angular/core';
import { MatIconRegistry } from '@angular/material/icon';
import { LoadingService } from './core/services/loading.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.scss',
})
export class App {
  readonly isLoading$;

  constructor(
    loadingService: LoadingService,
    matIconRegistry: MatIconRegistry,
  ) {
    this.isLoading$ = loadingService.isLoading$;
    matIconRegistry.setDefaultFontSetClass('material-symbols-outlined', 'mat-ligature-font', 'notranslate');
  }
}
