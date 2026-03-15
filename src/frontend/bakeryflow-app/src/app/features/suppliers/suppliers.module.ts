import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { SuppliersPageComponent } from './suppliers-page.component';

const routes: Routes = [{ path: '', component: SuppliersPageComponent }];

@NgModule({
  declarations: [SuppliersPageComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class SuppliersModule {}
