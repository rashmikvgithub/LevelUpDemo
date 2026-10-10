import { InjectionToken } from '@angular/core';

/** Base URL of the Level Up API. In development `/api` is proxied to the ASP.NET Core backend (see proxy.conf.json). */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api',
});
