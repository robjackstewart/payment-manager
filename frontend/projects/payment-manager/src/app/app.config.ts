import { ApplicationConfig, importProvidersFrom, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { ApiModule, Configuration, ConfigurationParameters } from '../../../payment-manager-api-client';
import { environment } from '../environments/environment';

export function paymentManagerApiConfigFactory(): Configuration {
  const params: ConfigurationParameters = {
    basePath: environment.apis.paymentManager.baseUrl,
  }
  return new Configuration(params);
}

export const appConfig: ApplicationConfig = {
  providers: [provideHttpClient(withInterceptorsFromDi()),
  importProvidersFrom(ApiModule.forRoot(paymentManagerApiConfigFactory)), provideZoneChangeDetection({ eventCoalescing: true }), provideRouter(routes)]
};
