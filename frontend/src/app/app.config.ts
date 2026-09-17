import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideApi } from '../api-client';
import { routes } from './app.routes';
import { environment } from '../environments/environment';

// Read the base path from the <base href> element — set at build time via Angular's baseHref
// option and substituted at container startup by the image entrypoint. Angular's router reads
// the same element automatically, so no explicit APP_BASE_HREF provider is needed.
const baseHref = document.querySelector('base')?.getAttribute('href') ?? '/';
const basePath = baseHref === '/' ? '' : baseHref.replace(/\/$/, '');

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(),
    provideApi(environment.apiBaseUrl ?? (window.location.origin + basePath)),
  ]
};

