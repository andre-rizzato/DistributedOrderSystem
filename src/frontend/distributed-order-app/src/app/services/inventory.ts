import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { AdjustInventoryRequest, SetInventoryRequest } from '../models/inventory';

/**
 * InventoryService
 *
 * Servizio Angular che gestisce tutte le operazioni HTTP relative all'inventario.
 * Comunica con il GatewayBff che funge da mediatore con l'InventoryService backend.
 *
 * Funzionalità principali:
 * - Aggiustare l'inventario (aggiungere/rimuovere quantità)
 * - Impostare l'inventario ad un valore specifico
 * - Gestione centralizzata degli errori HTTP
 */
@Injectable({
  providedIn: 'root',
})
export class InventoryService {
  // URL base del GatewayBff per i comandi inventario
  private readonly commandApiUrl = 'http://localhost:5189/api/commands/inventory';

  /**
   * Costruttore del servizio
   * @param http - Client HTTP di Angular per effettuare richieste HTTP
   */
  constructor(private http: HttpClient) { }

  /**
   * Aggiusta l'inventario di un prodotto
   *
   * Effettua una richiesta POST a /api/commands/inventory/adjust tramite il GatewayBff.
   * Il delta può essere positivo (aggiunge) o negativo (rimuove).
   *
   * @param request - Richiesta con productId e delta
   * @returns Observable<void>
   */
  adjustInventory(request: AdjustInventoryRequest): Observable<void> {
    return this.http.post<void>(`${this.commandApiUrl}/adjust`, request)
      .pipe(
        tap(() => console.log('Inventario aggiustato:', request)),
        catchError(this.handleError)
      );
  }

  /**
   * Imposta l'inventario di un prodotto ad un valore specifico
   *
   * Effettua una richiesta POST a /api/commands/inventory/set tramite il GatewayBff.
   *
   * @param request - Richiesta con productId e quantity
   * @returns Observable<void>
   */
  setInventory(request: SetInventoryRequest): Observable<void> {
    return this.http.post<void>(`${this.commandApiUrl}/set`, request)
      .pipe(
        tap(() => console.log('Inventario impostato:', request)),
        catchError(this.handleError)
      );
  }

  /**
   * Gestisce gli errori HTTP
   *
   * Metodo privato che processa gli errori HTTP e crea messaggi di errore leggibili.
   *
   * @param error - Oggetto di errore HTTP ricevuto
   * @returns Observable<never> - Observable che emette un errore
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'Si è verificato un errore';

    if (error.error instanceof ErrorEvent) {
      // Errore lato client
      errorMessage = `Errore: ${error.error.message}`;
    } else {
      // Errore lato server
      errorMessage = `Codice Errore: ${error.status}\nMessaggio: ${error.message}`;
    }

    console.error(errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}
