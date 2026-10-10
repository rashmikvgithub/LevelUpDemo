import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

/** Extracts a human-readable message from an RFC 7807 problem details response. */
export function getErrorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'An unexpected error occurred.';
  }
  if (error.status === 0) {
    return 'Cannot reach the Level Up API. Is the backend running?';
  }
  const problem = error.error as ProblemDetails | null;
  if (problem?.errors) {
    return Object.values(problem.errors).flat().join(' ');
  }
  return problem?.detail ?? problem?.title ?? error.message;
}

/** Shows a snackbar for every failed API call, then re-throws so callers can react (e.g. stop a spinner). */
export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);
  return next(req).pipe(
    catchError((error: unknown) => {
      notifications.error(getErrorMessage(error));
      return throwError(() => error);
    }),
  );
};
