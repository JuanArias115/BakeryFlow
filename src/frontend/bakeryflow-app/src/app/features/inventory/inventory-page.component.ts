import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { OptionItem, PagedResult } from '../../core/models/api.models';

interface InventoryStock {
  ingredientId: string;
  ingredientName: string;
  unitName: string;
  stockCurrent: number;
  stockMinimum: number;
  averageCost: number;
  isLowStock: boolean;
}

interface InventoryMovement {
  id: string;
  ingredientName: string;
  type: string;
  date: string;
  quantityIn: number;
  quantityOut: number;
  resultingBalance: number;
}

@Component({
  selector: 'app-inventory-page',
  templateUrl: './inventory-page.component.html',
  styleUrl: './inventory-page.component.scss',
  standalone: false,
})
export class InventoryPageComponent implements OnInit {
  stocks: InventoryStock[] = [];
  movements: InventoryMovement[] = [];
  ingredients: OptionItem[] = [];
  readonly form;

  constructor(
    private readonly fb: FormBuilder,
    private readonly apiService: ApiService,
  ) {
    this.form = this.fb.group({
      ingredientId: ['', Validators.required],
      quantityDelta: [0, Validators.required],
      unitCost: [0],
      notes: [''],
      date: [''],
    });
  }

  ngOnInit(): void {
    this.loadStocks();
    this.loadMovements();
    this.apiService.getOptions('ingredients').subscribe((items) => {
      this.ingredients = items;
    });
  }

  loadStocks(): void {
    this.apiService.getPaged<InventoryStock>('inventory/stocks', { page: 1, pageSize: 30 }).subscribe((result: PagedResult<InventoryStock>) => {
      this.stocks = result.items;
    });
  }

  loadMovements(): void {
    this.apiService.getPaged<InventoryMovement>('inventory/movements', { page: 1, pageSize: 20 }).subscribe((result: PagedResult<InventoryMovement>) => {
      this.movements = result.items;
    });
  }

  submitAdjustment(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.apiService.post('inventory/adjustments', value).subscribe(() => {
      this.form.reset({ ingredientId: '', quantityDelta: 0, unitCost: 0, notes: '', date: '' });
      this.loadStocks();
      this.loadMovements();
    });
  }
}
