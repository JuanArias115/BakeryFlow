import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { OptionItem, PagedResult } from '../models/api.models';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class CrudResourceService {
  constructor(private readonly apiService: ApiService) {}

  getPaged<T>(endpoint: string, params: Record<string, unknown>): Observable<PagedResult<T>> {
    return this.apiService.getPaged<T>(endpoint, params);
  }

  getOptions(endpoint: string): Observable<OptionItem[]> {
    return this.apiService.getOptions(endpoint);
  }

  create<T>(endpoint: string, payload: unknown): Observable<T> {
    return this.apiService.post<T>(endpoint, payload);
  }

  update<T>(endpoint: string, id: string, payload: unknown): Observable<T> {
    return this.apiService.put<T>(`${endpoint}/${id}`, payload);
  }

  toggleStatus(endpoint: string, id: string): Observable<boolean> {
    return this.apiService.patch<boolean>(`${endpoint}/${id}/toggle-status`);
  }
}
