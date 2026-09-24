import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';

import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

/**
 * Main Angular application configuration
 *
 * This file configures all of the application's global providers.
 * Providers are services and features that Angular makes available
 * throughout the application via the Dependency Injection system.
 *
 * Configured providers:
 *
 * 1. provideBrowserGlobalErrorListeners()
 *    - Provides global listeners to handle uncaught errors
 *    - Improves application-level error handling
 *
 * 2. provideZoneChangeDetection({ eventCoalescing: true })
 *    - Configures Angular's change detection system
 *    - eventCoalescing: batches multiple events to improve performance
 *
 * 3. provideRouter(routes)
 *    - Configures the application router with the defined routes
 *    - Enables navigation between pages (SPA)
 *
 * 4. provideClientHydration(withEventReplay())
 *    - Enables hydration for server-side rendering (SSR)
 *    - withEventReplay: replays user events after hydration
 *    - Improves the user experience during initial load
 *
 * 5. provideHttpClient(withFetch())
 *    - Configures HttpClient for making HTTP requests
 *    - withFetch: uses the browser's Fetch API instead of XMLHttpRequest
 *    - Required to communicate with the backend (ProductService)
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideClientHydration(withEventReplay()),
    provideHttpClient(withFetch())
  ]
};
