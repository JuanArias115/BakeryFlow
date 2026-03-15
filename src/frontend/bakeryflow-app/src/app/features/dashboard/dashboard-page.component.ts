import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../core/services/api.service';

interface DashboardTopItem {
  id: string;
  name: string;
  value: number;
  secondaryValue: number;
}

interface DashboardLowStock {
  ingredientId: string;
  ingredientName: string;
  stockCurrent: number;
  stockMinimum: number;
}

interface DashboardChartPoint {
  label: string;
  value: number;
  secondaryValue: number | null;
}

interface DashboardData {
  salesToday: number;
  salesMonth: number;
  purchasesMonth: number;
  productsCount: number;
  ingredientsCount: number;
  topProfitableProducts: DashboardTopItem[];
  topSellingProducts: DashboardTopItem[];
  lowStockIngredients: DashboardLowStock[];
  dailySalesChart: DashboardChartPoint[];
  monthlyFlowChart: DashboardChartPoint[];
}

@Component({
  selector: 'app-dashboard-page',
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  standalone: false,
})
export class DashboardPageComponent implements OnInit {
  loading = true;
  data: DashboardData | null = null;

  constructor(private readonly apiService: ApiService) {}

  ngOnInit(): void {
    this.apiService.get<DashboardData>('dashboard').subscribe({
      next: (data) => {
        this.data = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  currency(value: number): string {
    return new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'USD' }).format(value);
  }
}
