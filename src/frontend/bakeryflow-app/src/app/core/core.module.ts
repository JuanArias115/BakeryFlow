import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { AdminGuard } from './guards/admin.guard';
import { AuthGuard } from './guards/auth.guard';
import { authInterceptor } from './interceptors/auth.interceptor';
import { loadingInterceptor } from './interceptors/loading.interceptor';

@NgModule({
  providers: [AuthGuard, AdminGuard, provideHttpClient(withInterceptors([loadingInterceptor, authInterceptor]))],
})
export class CoreModule {}
