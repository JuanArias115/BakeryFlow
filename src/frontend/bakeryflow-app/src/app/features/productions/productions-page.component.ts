import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { PagedResult } from '../../core/models/api.models';

interface ProductionItem {
  id: string;
  productName: string;
  date: string;
  quantityToProduce: number;
  quantityActual: number;
  totalCost: number;
}

@Component({
  selector: 'app-productions-page',
  templateUrl: './productions-page.component.html',
  styleUrl: './productions-page.component.scss',
  standalone: false,
})
export class ProductionsPageComponent implements OnInit {
  items: ProductionItem[] = [];

  constructor(private readonly apiService: ApiService) {}

  ngOnInit(): void {
    this.apiService.getPaged<ProductionItem>('productions', { page: 1, pageSize: 20 }).subscribe((result: PagedResult<ProductionItem>) => {
      this.items = result.items;
    });
  }
}
