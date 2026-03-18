import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { DashboardPageComponent } from './dashboard-page.component';

@NgModule({
  declarations: [DashboardPageComponent],
  imports: [SharedModule],
  exports: [DashboardPageComponent],
})
export class DashboardModule {}
