import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { OptionItem, PagedResult } from '../../core/models/api.models';

interface RecipeItem {
  id: string;
  productId: string;
  productName: string;
  yield: number;
  yieldUnit: string;
  packagingCost: number;
  isActive: boolean;
  totalRecipeCost: number;
  unitCost: number;
}

interface RecipeCosting {
  productId: string;
  productName: string;
  salePrice: number;
  ingredientsCost: number;
  packagingCost: number;
  totalRecipeCost: number;
  yield: number;
  yieldUnit: string;
  unitCost: number;
  estimatedGrossProfit: number;
}

@Component({
  selector: 'app-recipes-page',
  templateUrl: './recipes-page.component.html',
  styleUrl: './recipes-page.component.scss',
  standalone: false,
})
export class RecipesPageComponent implements OnInit {
  recipes: RecipeItem[] = [];
  products: OptionItem[] = [];
  selectedProductId = '';
  costing: RecipeCosting | null = null;

  constructor(private readonly apiService: ApiService) {}

  ngOnInit(): void {
    this.apiService.getPaged<RecipeItem>('recipes', { page: 1, pageSize: 20 }).subscribe((result: PagedResult<RecipeItem>) => {
      this.recipes = result.items;
    });

    this.apiService.getOptions('products').subscribe((products) => {
      this.products = products;
    });
  }

  loadCosting(): void {
    if (!this.selectedProductId) {
      return;
    }

    this.apiService.get<RecipeCosting>(`recipes/costing/${this.selectedProductId}`).subscribe((value) => {
      this.costing = value;
    });
  }

  currency(value: number): string {
    return new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'USD' }).format(value);
  }
}
