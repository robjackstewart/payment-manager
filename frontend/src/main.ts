import { bootstrapApplication } from '@angular/platform-browser';
import { createAppConfig } from './app/app.config';
import { App } from './app/app';
import { environment } from './environments/environment';

async function bootstrap(): Promise<void> {
  let basePath = '';
  try {
    const response = await fetch('./proxy.json');
    if (response.ok) {
      const config = await response.json() as { basePath?: string };
      const raw = config.basePath ?? '';
      // Treat unsubstituted placeholder (dev/build) as empty — no base path.
      basePath = raw.startsWith('__') ? '' : raw;
    }
  } catch {
    // config.json not available; default to no base path.
  }

  // Dev environments provide a full API URL; in production the base path is the API prefix.
  const apiBaseUrl = environment.apiBaseUrl || basePath;

  bootstrapApplication(App, createAppConfig(apiBaseUrl, basePath))
    .catch(err => console.error(err));
}

bootstrap();
