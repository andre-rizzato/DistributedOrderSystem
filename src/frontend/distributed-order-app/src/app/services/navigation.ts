import { Injectable } from '@angular/core';
import { Router, NavigationError, NavigationEnd, NavigationStart } from '@angular/router';
import { filter } from 'rxjs/operators';

/**
 * NavigationService
 * 
 * Servizio per gestire errori di navigazione e fornire utilities per la navigazione.
 * Monitora gli eventi di routing e gestisce errori in modo centralizzato.
 */
@Injectable({
  providedIn: 'root'
})
export class NavigationService {
  private readonly navigationHistory: string[] = [];

  constructor(private router: Router) {
    this.initializeNavigationTracking();
  }

  /**
   * Inizializza il tracking degli eventi di navigazione
   */
  private initializeNavigationTracking(): void {
    // Traccia gli eventi di navigazione per debugging e gestione errori
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd || event instanceof NavigationError))
      .subscribe(event => {
        if (event instanceof NavigationEnd) {
          // Navigazione completata con successo
          this.navigationHistory.push(event.url);
          console.log('✅ Navigation successful:', event.url);
          
          // Mantieni solo gli ultimi 10 URL nella cronologia
          if (this.navigationHistory.length > 10) {
            this.navigationHistory.shift();
          }
        } else if (event instanceof NavigationError) {
          // Errore durante la navigazione
          console.error('❌ Navigation error:', event.error);
          this.handleNavigationError(event);
        }
      });
  }

  /**
   * Gestisce errori di navigazione in modo centralizzato
   */
  private handleNavigationError(event: NavigationError): void {
    console.error('Navigation failed:', {
      url: event.url,
      error: event.error,
      timestamp: new Date().toISOString()
    });

    // Mostra un messaggio di errore user-friendly
    this.showNavigationError(event.url);

    // Reindirizza alla pagina di errore o alla homepage
    this.router.navigate(['/not-found'], { 
      queryParams: { 
        originalUrl: event.url,
        error: 'navigation-failed' 
      }
    }).catch(fallbackError => {
      console.error('❌ Fallback navigation failed:', fallbackError);
      // Ultimo resort: reindirizzamento hard
      window.location.href = '/products';
    });
  }

  /**
   * Mostra un toast/alert per errori di navigazione
   */
  private showNavigationError(failedUrl: string): void {
    // In una app reale, qui useresti un servizio di notifiche/toast
    const message = `Navigation failed for: ${failedUrl}`;
    console.warn(message);
    
    // Opzione: mostra un alert nativo (solo per demo, in produzione usa un toast component)
    if (confirm(`${message}\\n\\nDo you want to go to the homepage?`)) {
      this.router.navigate(['/products']);
    }
  }

  /**
   * Naviga con gestione degli errori
   */
  navigateWithErrorHandling(route: string[] | string, extras?: any): Promise<boolean> {
    const routeArray = Array.isArray(route) ? route : [route];
    
    return this.router.navigate(routeArray, extras).catch(error => {
      console.error('❌ Manual navigation failed:', {
        route: routeArray,
        error: error,
        timestamp: new Date().toISOString()
      });
      
      // Mostra errore e reindirizza a fallback
      alert(`Navigation failed. Redirecting to homepage.`);
      return this.router.navigate(['/products']);
    });
  }

  /**
   * Ottiene la cronologia di navigazione
   */
  getNavigationHistory(): string[] {
    return [...this.navigationHistory];
  }

  /**
   * Ottiene l'URL precedente
   */
  getPreviousUrl(): string | null {
    const history = this.getNavigationHistory();
    return history.length >= 2 ? history[history.length - 2] : null;
  }

  /**
   * Verifica se una route esiste
   */
  async routeExists(url: string): Promise<boolean> {
    try {
      const result = await this.router.navigate([url], { skipLocationChange: true });
      return result;
    } catch (error) {
      console.warn('Route check failed:', error);
      return false;
    }
  }
}