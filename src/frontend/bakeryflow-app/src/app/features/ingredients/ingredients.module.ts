import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { IngredientsPageComponent } from './ingredients-page.component';

const routes: Routes = [{ path: '', component: IngredientsPageComponent }];

@NgModule({
  declarations: [IngredientsPageComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class IngredientsModule {}
