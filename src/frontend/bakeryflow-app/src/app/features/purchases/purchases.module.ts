import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { PurchasesPageComponent } from './purchases-page.component';

const routes: Routes = [{ path: '', component: PurchasesPageComponent }];

@NgModule({
  declarations: [PurchasesPageComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class PurchasesModule {}
