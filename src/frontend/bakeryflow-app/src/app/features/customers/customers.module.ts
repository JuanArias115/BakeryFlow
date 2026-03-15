import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { CustomersPageComponent } from './customers-page.component';

const routes: Routes = [{ path: '', component: CustomersPageComponent }];

@NgModule({
  declarations: [CustomersPageComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class CustomersModule {}
