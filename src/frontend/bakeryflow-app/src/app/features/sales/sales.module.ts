import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { SalesPageComponent } from './sales-page.component';

const routes: Routes = [{ path: '', component: SalesPageComponent }];

@NgModule({
  declarations: [SalesPageComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class SalesModule {}
