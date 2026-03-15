import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { ProductionsPageComponent } from './productions-page.component';

const routes: Routes = [{ path: '', component: ProductionsPageComponent }];

@NgModule({
  declarations: [ProductionsPageComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class ProductionsModule {}
