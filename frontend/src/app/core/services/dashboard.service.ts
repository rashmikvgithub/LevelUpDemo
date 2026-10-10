import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DashboardSummary, TrendPeriod, TrendResponse } from '../models/dashboard.models';
import { toQueryParams } from '../utils/date.utils';
import { API_BASE_URL } from './api-config';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/dashboard`;

  getSummary(date?: string): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.baseUrl}/summary`, { params: toQueryParams({ date }) });
  }

  getTrends(period: TrendPeriod, endDate?: string): Observable<TrendResponse> {
    return this.http.get<TrendResponse>(`${this.baseUrl}/trends`, { params: toQueryParams({ period, endDate }) });
  }
}
