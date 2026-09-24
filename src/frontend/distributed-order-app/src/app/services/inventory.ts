import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { AdjustInventoryRequest, SetInventoryRequest } from '../models/inventory';

/**
 * InventoryService
 *
 * Angular service that handles all HTTP operations related to inventory.
 * Talks to the GatewayBff, which acts as a mediator with the backend InventoryService.
 *
 * Main features:
 * - Adjust inventory (add/remove quantity)
 * - Set inventory to a specific value
 * - Centralized HTTP error handling
 */
@Injectable({
  providedIn: 'root',
})
export class InventoryService {
  // Base URL of the GatewayBff for inventory commands
  private readonly commandApiUrl = 'http://localhost:5189/api/commands/inventory';

  /**
   * Service constructor
   * @param http - Angular's HTTP client for making HTTP requests
   */
  constructor(private http: HttpClient) { }

  /**
   * Adjusts a product's inventory
   *
   * Makes a POST request to /api/commands/inventory/adjust through the GatewayBff.
   * The delta can be positive (adds) or negative (removes).
   *
   * @param request - Request with productId and delta
   * @returns Observable<void>
   */
  adjustInventory(request: AdjustInventoryRequest): Observable<void> {
    return this.http.post<void>(`${this.commandApiUrl}/adjust`, request)
      .pipe(
        tap(() => console.log('Inventory adjusted:', request)),
        catchError(this.handleError)
      );
  }

  /**
   * Sets a product's inventory to a specific value
   *
   * Makes a POST request to /api/commands/inventory/set through the GatewayBff.
   *
   * @param request - Request with productId and quantity
   * @returns Observable<void>
   */
  setInventory(request: SetInventoryRequest): Observable<void> {
    return this.http.post<void>(`${this.commandApiUrl}/set`, request)
      .pipe(
        tap(() => console.log('Inventory set:', request)),
        catchError(this.handleError)
      );
  }

  /**
   * Handles HTTP errors
   *
   * Private method that processes HTTP errors and builds readable error messages.
   *
   * @param error - The received HTTP error object
   * @returns Observable<never> - Observable that emits an error
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An error occurred';

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Server-side error
      errorMessage = `Error code: ${error.status}\nMessage: ${error.message}`;
    }

    console.error(errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}
