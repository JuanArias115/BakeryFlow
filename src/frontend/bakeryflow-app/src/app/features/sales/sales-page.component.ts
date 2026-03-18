import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { PagedResult } from '../../core/models/api.models';
import { formatCopCurrency } from '../../shared/utils/currency';

interface SaleItem {
  id: string;
  customerName: string | null;
  date: string;
  total: number;
  profit: number;
  paymentMethod: string;
}

@Component({
  selector: 'app-sales-page',
  templateUrl: './sales-page.component.html',
  styleUrl: './sales-page.component.scss',
  standalone: false,
})
export class SalesPageComponent implements OnInit {
  items: SaleItem[] = [];

  constructor(private readonly apiService: ApiService) {}

  ngOnInit(): void {
    this.apiService.getPaged<SaleItem>('sales', { page: 1, pageSize: 20 }).subscribe((result: PagedResult<SaleItem>) => {
      this.items = result.items;
    });
  }

  currency(value: number): string {
    return formatCopCurrency(value);
  }
}
