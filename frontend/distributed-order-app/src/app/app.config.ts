import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';

import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

/**
 * Configurazione principale dell'applicazione Angular
 *
 * Questo file configura tutti i provider globali dell'applicazione.
 * I provider sono servizi e funzionalità che Angular rende disponibili
 * in tutta l'applicazione attraverso il sistema di Dependency Injection.
 *
 * Provider configurati:
 *
 * 1. provideBrowserGlobalErrorListeners()
 *    - Fornisce listener globali per gestire errori non catturati
 *    - Migliora la gestione degli errori a livello di applicazione
 *
 * 2. provideZoneChangeDetection({ eventCoalescing: true })
 *    - Configura il sistema di change detection di Angular
 *    - eventCoalescing: raggruppa più eventi per migliorare le performance
 *
 * 3. provideRouter(routes)
 *    - Configura il router dell'applicazione con le routes definite
 *    - Abilita la navigazione tra le pagine (SPA)
 *
 * 4. provideClientHydration(withEventReplay())
 *    - Abilita l'hydration per il rendering server-side (SSR)
 *    - withEventReplay: rigioca gli eventi utente dopo l'hydration
 *    - Migliora l'esperienza utente durante il caricamento iniziale
 *
 * 5. provideHttpClient(withFetch())
 *    - Configura l'HttpClient per effettuare richieste HTTP
 *    - withFetch: usa l'API Fetch del browser invece di XMLHttpRequest
 *    - Necessario per comunicare con il backend (ProductService)
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
