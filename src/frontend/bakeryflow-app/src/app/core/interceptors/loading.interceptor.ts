import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { LoadingService } from '../services/loading.service';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);
  const skipLoading = req.headers.has('X-Skip-Loader');

  if (!skipLoading) {
    loadingService.start();
  }

  return next(req).pipe(
    finalize(() => {
      if (!skipLoading) {
        loadingService.stop();
      }
    }),
  );
};
