import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-reports-page',
  templateUrl: './reports-page.component.html',
  styleUrl: './reports-page.component.scss',
  standalone: false,
})
export class ReportsPageComponent implements OnInit {
  purchases: unknown[] = [];
  sales: unknown[] = [];
  inventory: unknown[] = [];
  costs: unknown[] = [];
  profitability: unknown[] = [];
  loading = true;
  error = '';

  readonly filters;

  constructor(
    private readonly fb: FormBuilder,
    private readonly apiService: ApiService,
    private readonly http: HttpClient,
  ) {
    this.filters = this.fb.group({
      from: [''],
      to: [''],
    });
  }

  ngOnInit(): void {
    this.loadReports();
  }

  loadReports(): void {
    this.loading = true;
    this.error = '';
    const filters = this.filters.getRawValue();

    forkJoin({
      purchases: this.apiService.get<unknown[]>('reports/purchases', filters),
      sales: this.apiService.get<unknown[]>('reports/sales', filters),
      inventory: this.apiService.get<unknown[]>('reports/inventory'),
      costs: this.apiService.get<unknown[]>('reports/product-costs'),
      profitability: this.apiService.get<unknown[]>('reports/product-profitability', filters),
    }).subscribe({
      next: (result) => {
        this.purchases = result.purchases;
        this.sales = result.sales;
        this.inventory = result.inventory;
        this.costs = result.costs;
        this.profitability = result.profitability;
        this.loading = false;
      },
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'No se pudieron cargar los reportes.';
        this.loading = false;
      },
    });
  }

  downloadCsv(endpoint: string, fileName: string): void {
    this.http
      .get(`${environment.apiUrl}/reports/${endpoint}/csv`, {
        params: this.filters.getRawValue() as Record<string, string>,
        responseType: 'blob',
      })
      .subscribe((blob) => {
        const url = window.URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = fileName;
        anchor.click();
        window.URL.revokeObjectURL(url);
      });
  }

  formatJson(value: unknown[]): string {
    return JSON.stringify(value, null, 2);
  }
}
