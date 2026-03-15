import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { environment } from '../../../environments/environment';

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
    const filters = this.filters.getRawValue();
    this.apiService.get<unknown[]>('reports/purchases', filters).subscribe((data) => (this.purchases = data));
    this.apiService.get<unknown[]>('reports/sales', filters).subscribe((data) => (this.sales = data));
    this.apiService.get<unknown[]>('reports/inventory').subscribe((data) => (this.inventory = data));
    this.apiService.get<unknown[]>('reports/product-costs').subscribe((data) => (this.costs = data));
    this.apiService.get<unknown[]>('reports/product-profitability', filters).subscribe((data) => (this.profitability = data));
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
}
