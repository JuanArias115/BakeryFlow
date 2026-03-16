import { Component } from '@angular/core';
import { LoadingService } from './core/services/loading.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.scss',
})
export class App {
  readonly isLoading$;

  constructor(loadingService: LoadingService) {
    this.isLoading$ = loadingService.isLoading$;
  }
}
