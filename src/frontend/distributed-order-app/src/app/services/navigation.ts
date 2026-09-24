import { Injectable } from '@angular/core';
import { Router, NavigationError, NavigationEnd, NavigationStart } from '@angular/router';
import { filter } from 'rxjs/operators';

/**
 * NavigationService
 *
 * Service for handling navigation errors and providing navigation utilities.
 * Monitors routing events and handles errors centrally.
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
   * Initializes navigation event tracking
   */
  private initializeNavigationTracking(): void {
    // Track navigation events for debugging and error handling
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd || event instanceof NavigationError))
      .subscribe(event => {
        if (event instanceof NavigationEnd) {
          // Navigation completed successfully
          this.navigationHistory.push(event.url);
          console.log('✅ Navigation successful:', event.url);

          // Keep only the last 10 URLs in the history
          if (this.navigationHistory.length > 10) {
            this.navigationHistory.shift();
          }
        } else if (event instanceof NavigationError) {
          // Error during navigation
          console.error('❌ Navigation error:', event.error);
          this.handleNavigationError(event);
        }
      });
  }

  /**
   * Handles navigation errors centrally
   */
  private handleNavigationError(event: NavigationError): void {
    console.error('Navigation failed:', {
      url: event.url,
      error: event.error,
      timestamp: new Date().toISOString()
    });

    // Show a user-friendly error message
    this.showNavigationError(event.url);

    // Redirect to the error page or the homepage
    this.router.navigate(['/not-found'], {
      queryParams: {
        originalUrl: event.url,
        error: 'navigation-failed'
      }
    }).catch(fallbackError => {
      console.error('❌ Fallback navigation failed:', fallbackError);
      // Last resort: hard redirect
      window.location.href = '/products';
    });
  }

  /**
   * Shows a toast/alert for navigation errors
   */
  private showNavigationError(failedUrl: string): void {
    // In a real app, you'd use a notification/toast service here
    const message = `Navigation failed for: ${failedUrl}`;
    console.warn(message);

    // Option: show a native alert (demo only, use a toast component in production)
    if (confirm(`${message}\\n\\nDo you want to go to the homepage?`)) {
      this.router.navigate(['/products']);
    }
  }

  /**
   * Navigates with error handling
   */
  navigateWithErrorHandling(route: string[] | string, extras?: any): Promise<boolean> {
    const routeArray = Array.isArray(route) ? route : [route];

    return this.router.navigate(routeArray, extras).catch(error => {
      console.error('❌ Manual navigation failed:', {
        route: routeArray,
        error: error,
        timestamp: new Date().toISOString()
      });

      // Show an error and redirect to fallback
      alert(`Navigation failed. Redirecting to homepage.`);
      return this.router.navigate(['/products']);
    });
  }

  /**
   * Gets the navigation history
   */
  getNavigationHistory(): string[] {
    return [...this.navigationHistory];
  }

  /**
   * Gets the previous URL
   */
  getPreviousUrl(): string | null {
    const history = this.getNavigationHistory();
    return history.length >= 2 ? history[history.length - 2] : null;
  }

  /**
   * Checks whether a route exists
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
