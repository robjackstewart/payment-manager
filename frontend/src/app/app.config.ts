import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { APP_BASE_HREF } from '@angular/common';
import { provideApi } from '../api-client';
import { routes } from './app.routes';

export function createAppConfig(apiBaseUrl: string, basePath: string): ApplicationConfig {
  return {
    providers: [
      provideZonelessChangeDetection(),
      provideRouter(routes),
      provideHttpClient(withFetch()),
      provideApi(apiBaseUrl),
      ...(basePath ? [{ provide: APP_BASE_HREF, useValue: basePath + '/' }] : []),
    ]
  };
}
