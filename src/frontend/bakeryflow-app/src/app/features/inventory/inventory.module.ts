import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { InventoryPageComponent } from './inventory-page.component';

const routes: Routes = [{ path: '', component: InventoryPageComponent }];

@NgModule({
  declarations: [InventoryPageComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class InventoryModule {}
