import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminGuard } from './core/guards/admin.guard';
import { AuthGuard } from './core/guards/auth.guard';
import { ShellComponent } from './shared/layout/shell.component';

const routes: Routes = [
  {
    path: 'login',
    loadChildren: () => import('./features/auth/auth.module').then((m) => m.AuthModule),
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [AuthGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.module').then((m) => m.DashboardModule),
      },
      {
        path: 'categories',
        loadChildren: () => import('./features/categories/categories.module').then((m) => m.CategoriesModule),
      },
      {
        path: 'products',
        loadChildren: () => import('./features/products/products.module').then((m) => m.ProductsModule),
      },
      {
        path: 'ingredients',
        loadChildren: () => import('./features/ingredients/ingredients.module').then((m) => m.IngredientsModule),
      },
      {
        path: 'units',
        loadChildren: () => import('./features/units/units.module').then((m) => m.UnitsModule),
      },
      {
        path: 'suppliers',
        loadChildren: () => import('./features/suppliers/suppliers.module').then((m) => m.SuppliersModule),
      },
      {
        path: 'customers',
        loadChildren: () => import('./features/customers/customers.module').then((m) => m.CustomersModule),
      },
      {
        path: 'recipes',
        loadChildren: () => import('./features/recipes/recipes.module').then((m) => m.RecipesModule),
      },
      {
        path: 'purchases',
        loadChildren: () => import('./features/purchases/purchases.module').then((m) => m.PurchasesModule),
      },
      {
        path: 'inventory',
        loadChildren: () => import('./features/inventory/inventory.module').then((m) => m.InventoryModule),
      },
      {
        path: 'productions',
        loadChildren: () => import('./features/productions/productions.module').then((m) => m.ProductionsModule),
      },
      {
        path: 'sales',
        loadChildren: () => import('./features/sales/sales.module').then((m) => m.SalesModule),
      },
      {
        path: 'reports',
        loadChildren: () => import('./features/reports/reports.module').then((m) => m.ReportsModule),
      },
      {
        path: 'users',
        canActivate: [AdminGuard],
        loadChildren: () => import('./features/users/users.module').then((m) => m.UsersModule),
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
