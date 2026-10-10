import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Recommendation,
  RecommendationCategory,
  RecommendationHistory,
  RecommendationRequest,
  SaveRecommendationRequest,
  ShownRecommendation,
} from '../models/recommendation.models';
import { toQueryParams } from '../utils/date.utils';
import { API_BASE_URL } from './api-config';

@Injectable({ providedIn: 'root' })
export class RecommendationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/recommendations`;

  list(category?: RecommendationCategory | null, includeInactive = true): Observable<Recommendation[]> {
    return this.http.get<Recommendation[]>(this.baseUrl, { params: toQueryParams({ category, includeInactive }) });
  }

  create(request: SaveRecommendationRequest): Observable<Recommendation> {
    return this.http.post<Recommendation>(this.baseUrl, request);
  }

  update(id: number, request: SaveRecommendationRequest): Observable<Recommendation> {
    return this.http.put<Recommendation>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** "Help Me!" - fetches a suggestion for the given context (recorded in history server-side). */
  suggest(request: RecommendationRequest): Observable<ShownRecommendation> {
    return this.http.post<ShownRecommendation>(`${this.baseUrl}/suggest`, request);
  }

  history(activityId?: number | null, goalId?: number | null, take = 50): Observable<RecommendationHistory[]> {
    return this.http.get<RecommendationHistory[]>(`${this.baseUrl}/history`, {
      params: toQueryParams({ activityId, goalId, take }),
    });
  }
}
