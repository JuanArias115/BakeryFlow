import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { UnitsPageComponent } from './units-page.component';

const routes: Routes = [{ path: '', component: UnitsPageComponent }];

@NgModule({
  declarations: [UnitsPageComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class UnitsModule {}
