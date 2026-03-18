import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { formatCopCurrency } from '../../shared/utils/currency';

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
  error = '';
  data: DashboardData | null = null;

  constructor(private readonly apiService: ApiService) {}

  ngOnInit(): void {
    this.apiService.get<DashboardData>('dashboard').subscribe({
      next: (data) => {
        this.data = data;
        this.error = '';
        this.loading = false;
      },
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'No se pudo cargar el dashboard.';
        this.data = null;
        this.loading = false;
      },
    });
  }

  currency(value: number): string {
    return formatCopCurrency(value);
  }

  chartWidth(value: number, maxValue: number): number {
    if (!maxValue) {
      return 0;
    }

    return Math.max(8, (value / maxValue) * 100);
  }

  get hasOperationalData(): boolean {
    if (!this.data) {
      return false;
    }

    return (
      this.data.salesToday > 0 ||
      this.data.salesMonth > 0 ||
      this.data.purchasesMonth > 0 ||
      this.data.topProfitableProducts.length > 0 ||
      this.data.topSellingProducts.length > 0 ||
      this.data.lowStockIngredients.length > 0
    );
  }

  get dailySalesMax(): number {
    return Math.max(...(this.data?.dailySalesChart.map((point) => point.value) ?? [0]));
  }

  get monthlyFlowMax(): number {
    return Math.max(
      ...(this.data?.monthlyFlowChart.flatMap((point) => [point.value, point.secondaryValue ?? 0]) ?? [0]),
    );
  }
}
