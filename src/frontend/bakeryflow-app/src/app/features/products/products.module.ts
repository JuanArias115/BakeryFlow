import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { ProductsPageComponent } from './products-page.component';

const routes: Routes = [{ path: '', component: ProductsPageComponent }];

@NgModule({
  declarations: [ProductsPageComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class ProductsModule {}
