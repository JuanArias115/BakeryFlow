import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { UsersPageComponent } from './users-page.component';

const routes: Routes = [{ path: '', component: UsersPageComponent }];

@NgModule({
  declarations: [UsersPageComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class UsersModule {}
