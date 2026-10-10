import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'dashboard',
    title: 'Dashboard | Level-UP My Life',
    loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'activities',
    title: 'Activities | Level-UP My Life',
    loadComponent: () => import('./features/activities/activity-list').then((m) => m.ActivityList),
  },
  {
    path: 'activities/:id',
    title: 'Activity details | Level-UP My Life',
    loadComponent: () => import('./features/activities/activity-detail').then((m) => m.ActivityDetail),
  },
  {
    path: 'goals',
    title: 'Goals | Level-UP My Life',
    loadComponent: () => import('./features/goals/goal-list').then((m) => m.GoalList),
  },
  {
    path: 'goals/:id',
    title: 'Goal details | Level-UP My Life',
    loadComponent: () => import('./features/goals/goal-detail').then((m) => m.GoalDetail),
  },
  {
    path: 'recommendations',
    title: 'Recommendations | Level-UP My Life',
    loadComponent: () => import('./features/recommendations/recommendation-list').then((m) => m.RecommendationList),
  },
  { path: '**', redirectTo: 'dashboard' },
];
