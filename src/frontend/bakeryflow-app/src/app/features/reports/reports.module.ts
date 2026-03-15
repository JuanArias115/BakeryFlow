import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { ReportsPageComponent } from './reports-page.component';

const routes: Routes = [{ path: '', component: ReportsPageComponent }];

@NgModule({
  declarations: [ReportsPageComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class ReportsModule {}
