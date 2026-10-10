import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Activity,
  ActivityQuery,
  ActivityScore,
  CreateActivityRequest,
  DailyActivity,
  SaveActivityScoreRequest,
  SaveActivityScoreResult,
  UpdateActivityRequest,
} from '../models/activity.models';
import { toQueryParams } from '../utils/date.utils';
import { API_BASE_URL } from './api-config';

@Injectable({ providedIn: 'root' })
export class ActivityService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/activities`;

  search(query: ActivityQuery = {}): Observable<Activity[]> {
    return this.http.get<Activity[]>(this.baseUrl, { params: toQueryParams(query) });
  }

  getById(id: number): Observable<Activity> {
    return this.http.get<Activity>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateActivityRequest): Observable<Activity> {
    return this.http.post<Activity>(this.baseUrl, request);
  }

  update(id: number, request: UpdateActivityRequest): Observable<Activity> {
    return this.http.put<Activity>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  getDaily(date: string): Observable<DailyActivity[]> {
    return this.http.get<DailyActivity[]>(`${this.baseUrl}/daily`, { params: { date } });
  }

  getScores(activityId: number, from?: string, to?: string): Observable<ActivityScore[]> {
    return this.http.get<ActivityScore[]>(`${this.baseUrl}/${activityId}/scores`, {
      params: toQueryParams({ from, to }),
    });
  }

  saveScore(activityId: number, request: SaveActivityScoreRequest): Observable<SaveActivityScoreResult> {
    return this.http.put<SaveActivityScoreResult>(`${this.baseUrl}/${activityId}/scores`, request);
  }

  deleteScore(activityId: number, scoreId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${activityId}/scores/${scoreId}`);
  }
}
