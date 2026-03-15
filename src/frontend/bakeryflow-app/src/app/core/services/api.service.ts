import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, OptionItem, PagedResult } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  get<T>(endpoint: string, params?: Record<string, unknown>): Observable<T> {
    return this.http
      .get<ApiResponse<T>>(this.buildUrl(endpoint), { params: this.toHttpParams(params) })
      .pipe(map((response) => response.data));
  }

  post<T>(endpoint: string, payload: unknown): Observable<T> {
    return this.http
      .post<ApiResponse<T>>(this.buildUrl(endpoint), payload)
      .pipe(map((response) => response.data));
  }

  put<T>(endpoint: string, payload: unknown): Observable<T> {
    return this.http
      .put<ApiResponse<T>>(this.buildUrl(endpoint), payload)
      .pipe(map((response) => response.data));
  }

  patch<T>(endpoint: string, payload?: unknown): Observable<T> {
    return this.http
      .patch<ApiResponse<T>>(this.buildUrl(endpoint), payload ?? {})
      .pipe(map((response) => response.data));
  }

  getPaged<T>(endpoint: string, params: Record<string, unknown>): Observable<PagedResult<T>> {
    return this.get<PagedResult<T>>(endpoint, params);
  }

  getOptions(endpoint: string): Observable<OptionItem[]> {
    return this.get<OptionItem[]>(`${endpoint}/options`);
  }

  private buildUrl(endpoint: string): string {
    return `${this.baseUrl}/${endpoint}`.replace(/([^:]\/)\/+/g, '$1');
  }

  private toHttpParams(params?: Record<string, unknown>): HttpParams | undefined {
    if (!params) {
      return undefined;
    }

    let httpParams = new HttpParams();
    Object.entries(params)
      .filter(([, value]) => value !== undefined && value !== null && value !== '')
      .forEach(([key, value]) => {
        httpParams = httpParams.set(key, String(value));
      });

    return httpParams;
  }
}
