import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { PagedResult } from '../../core/models/api.models';
import { formatCopCurrency } from '../../shared/utils/currency';

interface PurchaseItem {
  id: string;
  supplierName: string;
  invoiceNumber: string | null;
  purchaseDate: string;
  total: number;
  status: string;
}

@Component({
  selector: 'app-purchases-page',
  templateUrl: './purchases-page.component.html',
  styleUrl: './purchases-page.component.scss',
  standalone: false,
})
export class PurchasesPageComponent implements OnInit {
  items: PurchaseItem[] = [];

  constructor(private readonly apiService: ApiService) {}

  ngOnInit(): void {
    this.apiService.getPaged<PurchaseItem>('purchases', { page: 1, pageSize: 20 }).subscribe((result: PagedResult<PurchaseItem>) => {
      this.items = result.items;
    });
  }

  currency(value: number): string {
    return formatCopCurrency(value);
  }
}
