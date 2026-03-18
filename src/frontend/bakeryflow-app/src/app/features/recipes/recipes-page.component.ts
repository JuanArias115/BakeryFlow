import { Component, OnInit, TemplateRef, ViewChild, inject } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { finalize, forkJoin } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { OptionItem, PagedResult } from '../../core/models/api.models';
import { formatCopCurrency } from '../../shared/utils/currency';

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

interface RecipeDetail {
  id: string;
  productId: string;
  yield: number;
  yieldUnit: string;
  packagingCost: number;
  notes: string | null;
  isActive: boolean;
  details: Array<{
    ingredientId: string;
    unitOfMeasureId: string;
    quantity: number;
  }>;
}

interface IngredientOption {
  id: string;
  name: string;
  unitOfMeasureId: string;
  unitName: string;
}

@Component({
  selector: 'app-recipes-page',
  templateUrl: './recipes-page.component.html',
  styleUrl: './recipes-page.component.scss',
  standalone: false,
})
export class RecipesPageComponent implements OnInit {
  @ViewChild('costingDialogTemplate') costingDialogTemplate?: TemplateRef<unknown>;
  @ViewChild('recipeDialogTemplate') recipeDialogTemplate?: TemplateRef<unknown>;

  private readonly apiService = inject(ApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);

  recipes: RecipeItem[] = [];
  products: OptionItem[] = [];
  ingredients: IngredientOption[] = [];
  selectedProductId = '';
  costing: RecipeCosting | null = null;
  loading = true;
  submitting = false;
  error = '';
  editingId: string | null = null;
  private costingDialogRef: MatDialogRef<unknown> | null = null;
  private recipeDialogRef: MatDialogRef<unknown> | null = null;

  readonly form = this.formBuilder.group({
    productId: ['', Validators.required],
    yield: [1, [Validators.required, Validators.min(0.01)]],
    yieldUnit: ['unidades', Validators.required],
    packagingCost: [0, [Validators.required, Validators.min(0)]],
    notes: [''],
    isActive: [true],
    details: this.formBuilder.array<FormGroup>([]),
  });

  get details(): FormArray<FormGroup> {
    return this.form.controls.details;
  }

  ngOnInit(): void {
    this.loadReferenceData();
    this.loadRecipes();
  }

  openCreateDialog(): void {
    this.editingId = null;
    this.error = '';
    this.resetForm();
    this.openRecipeDialog();
  }

  edit(recipeId: string): void {
    this.editingId = recipeId;
    this.error = '';
    this.submitting = true;

    this.apiService
      .get<RecipeDetail>(`recipes/${recipeId}`)
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: (recipe) => {
          this.form.reset({
            productId: recipe.productId,
            yield: recipe.yield,
            yieldUnit: recipe.yieldUnit,
            packagingCost: recipe.packagingCost,
            notes: recipe.notes ?? '',
            isActive: recipe.isActive,
          });
          this.details.clear();
          recipe.details.forEach((detail) => {
            const ingredient = this.ingredients.find((item) => item.id === detail.ingredientId);
            this.details.push(
              this.formBuilder.group({
                ingredientId: [detail.ingredientId, Validators.required],
                quantity: [detail.quantity, [Validators.required, Validators.min(0.01)]],
                unitOfMeasureId: [detail.unitOfMeasureId, Validators.required],
                unitName: [ingredient?.unitName ?? '—'],
              }),
            );
          });
          this.openRecipeDialog();
        },
        error: (response) => {
          this.error = response?.error?.message ?? 'No fue posible cargar la receta.';
        },
      });
  }

  openCostingDialog(): void {
    if (!this.costingDialogTemplate) {
      return;
    }

    this.costingDialogRef = this.dialog.open(this.costingDialogTemplate, {
      width: 'min(840px, calc(100vw - 2rem))',
      maxWidth: 'calc(100vw - 2rem)',
      panelClass: 'bf-dialog-panel',
      autoFocus: false,
    });

    this.costingDialogRef.afterClosed().subscribe(() => {
      this.costingDialogRef = null;
    });
  }

  closeCostingDialog(): void {
    this.costingDialogRef?.close();
    this.costingDialogRef = null;
  }

  closeRecipeDialog(): void {
    this.recipeDialogRef?.close();
    this.recipeDialogRef = null;
  }

  addLine(): void {
    this.details.push(
      this.formBuilder.group({
        ingredientId: ['', Validators.required],
        quantity: [1, [Validators.required, Validators.min(0.01)]],
        unitOfMeasureId: ['', Validators.required],
        unitName: [''],
      }),
    );
  }

  removeLine(index: number): void {
    if (this.details.length === 1) {
      return;
    }

    this.details.removeAt(index);
  }

  onIngredientChange(index: number): void {
    const group = this.details.at(index);
    const ingredient = this.ingredients.find((item) => item.id === group.get('ingredientId')?.value);
    if (!ingredient) {
      return;
    }

    group.patchValue(
      {
        unitOfMeasureId: ingredient.unitOfMeasureId,
        unitName: ingredient.unitName,
      },
      { emitEvent: false },
    );
  }

  submitRecipe(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.error = '';

    const request = this.editingId
      ? this.apiService.put(`recipes/${this.editingId}`, this.form.getRawValue())
      : this.apiService.post('recipes', this.form.getRawValue());

    request
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: () => {
          this.closeRecipeDialog();
          this.loadRecipes();
        },
        error: (response) => {
          this.error = response?.error?.message ?? 'No fue posible guardar la receta.';
        },
      });
  }

  loadCosting(): void {
    if (!this.selectedProductId) {
      return;
    }

    this.apiService.get<RecipeCosting>(`recipes/costing/${this.selectedProductId}`).subscribe({
      next: (value) => {
        this.costing = value;
      },
      error: (response) => {
        this.error = response?.error?.message ?? 'No fue posible consultar el costeo.';
      },
    });
  }

  currency(value: number): string {
    return formatCopCurrency(value);
  }

  private openRecipeDialog(): void {
    if (!this.recipeDialogTemplate) {
      return;
    }

    this.recipeDialogRef = this.dialog.open(this.recipeDialogTemplate, {
      width: 'min(960px, calc(100vw - 2rem))',
      maxWidth: 'calc(100vw - 2rem)',
      panelClass: 'bf-dialog-panel',
      autoFocus: false,
    });

    this.recipeDialogRef.afterClosed().subscribe(() => {
      this.recipeDialogRef = null;
    });
  }

  private loadReferenceData(): void {
    forkJoin({
      products: this.apiService.getOptions('products'),
      ingredients: this.apiService.getPaged<{
        id: string;
        name: string;
        unitOfMeasureId: string;
        unitName: string;
      }>('ingredients', { page: 1, pageSize: 100 }),
    }).subscribe({
      next: ({ products, ingredients }) => {
        this.products = products;
        this.ingredients = ingredients.items.map((item) => ({
          id: item.id,
          name: item.name,
          unitOfMeasureId: item.unitOfMeasureId,
          unitName: item.unitName,
        }));
      },
      error: () => {
        this.error = 'No fue posible cargar productos e ingredientes.';
      },
    });
  }

  private loadRecipes(): void {
    this.loading = true;

    this.apiService
      .getPaged<RecipeItem>('recipes', { page: 1, pageSize: 30 })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (result: PagedResult<RecipeItem>) => {
          this.recipes = result.items;
        },
        error: () => {
          this.error = 'No fue posible cargar las recetas.';
        },
      });
  }

  private resetForm(): void {
    this.form.reset({
      productId: '',
      yield: 1,
      yieldUnit: 'unidades',
      packagingCost: 0,
      notes: '',
      isActive: true,
    });
    this.details.clear();
    this.addLine();
  }
}
