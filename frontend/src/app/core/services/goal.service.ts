import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateGoalCheckpointRequest, Goal, GoalCheckpoint, GoalQuery, SaveGoalRequest } from '../models/goal.models';
import { toQueryParams } from '../utils/date.utils';
import { API_BASE_URL } from './api-config';

@Injectable({ providedIn: 'root' })
export class GoalService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/goals`;

  search(query: GoalQuery = {}): Observable<Goal[]> {
    return this.http.get<Goal[]>(this.baseUrl, { params: toQueryParams(query) });
  }

  getById(id: number): Observable<Goal> {
    return this.http.get<Goal>(`${this.baseUrl}/${id}`);
  }

  create(request: SaveGoalRequest): Observable<Goal> {
    return this.http.post<Goal>(this.baseUrl, request);
  }

  update(id: number, request: SaveGoalRequest): Observable<Goal> {
    return this.http.put<Goal>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  complete(id: number, note?: string | null): Observable<Goal> {
    return this.http.post<Goal>(`${this.baseUrl}/${id}/complete`, { note: note ?? null });
  }

  getCheckpoints(goalId: number): Observable<GoalCheckpoint[]> {
    return this.http.get<GoalCheckpoint[]>(`${this.baseUrl}/${goalId}/checkpoints`);
  }

  addCheckpoint(goalId: number, request: CreateGoalCheckpointRequest): Observable<GoalCheckpoint> {
    return this.http.post<GoalCheckpoint>(`${this.baseUrl}/${goalId}/checkpoints`, request);
  }

  deleteCheckpoint(goalId: number, checkpointId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${goalId}/checkpoints/${checkpointId}`);
  }
}
