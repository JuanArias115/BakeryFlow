import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { RecipesPageComponent } from './recipes-page.component';

const routes: Routes = [{ path: '', component: RecipesPageComponent }];

@NgModule({
  declarations: [RecipesPageComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class RecipesModule {}
