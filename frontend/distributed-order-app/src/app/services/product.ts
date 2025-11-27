import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap, map } from 'rxjs/operators';
import { Product, CreateProductRequest, UpdateProductRequest, CatalogItem } from '../models/product';

/**
 * ProductService
 *
 * Servizio Angular che gestisce tutte le operazioni HTTP relative ai prodotti.
 * Questo servizio è un singleton (providedIn: 'root') condiviso in tutta l'applicazione.
 *
 * Funzionalità principali:
 * - Recuperare tutti i prodotti dal catalogo (con informazioni inventario)
 * - Recuperare un singolo prodotto per ID
 * - Creare un nuovo prodotto
 * - Aggiornare un prodotto esistente
 * - Eliminare un prodotto
 * - Gestione centralizzata degli errori HTTP
 *
 * Tutte le operazioni comunicano con il GatewayBff (Backend for Frontend)
 * in esecuzione su localhost:5189, che funge da mediatore tra il frontend
 * e i microservizi backend (ProductService, InventoryService, ecc.).
 */
@Injectable({
  providedIn: 'root',
})
export class ProductService {
  // URL base del GatewayBff per le query (lettura)
  private readonly queryApiUrl = 'http://localhost:5189/api/queries';
  // URL base del GatewayBff per i comandi (scrittura)
  private readonly commandApiUrl = 'http://localhost:5189/api/commands';

  /**
   * Costruttore del servizio
   * @param http - Client HTTP di Angular per effettuare richieste HTTP
   */
  constructor(private http: HttpClient) { }

  /**
   * Recupera tutti i prodotti dal catalogo con informazioni inventario
   *
   * Effettua una richiesta GET a /api/queries/catalog tramite il GatewayBff.
   * Il BFF aggrega dati da ProductService e InventoryService.
   *
   * @returns Observable<CatalogItem[]> - Array di elementi del catalogo con quantità disponibile
   */
  getAllProducts(): Observable<CatalogItem[]> {
    return this.http.get<CatalogItem[]>(`${this.queryApiUrl}/catalog`)
      .pipe(
        tap(products => console.log('Prodotti catalogo recuperati:', products)),
        catchError(this.handleError)
      );
  }

  /**
   * Recupera un singolo prodotto tramite il suo ID
   *
   * Effettua una richiesta GET a /api/queries/catalog/{id} tramite il GatewayBff.
   * @param id - ID del prodotto da recuperare
   * @returns Observable<CatalogItem> - Il prodotto richiesto con dati inventario
   */
  getProductById(id: number): Observable<CatalogItem> {
    return this.http.get<CatalogItem>(`${this.queryApiUrl}/catalog/${id}`)
      .pipe(
        tap(product => console.log('Prodotto recuperato:', product)),
        catchError(this.handleError)
      );
  }

  /**
   * Crea un nuovo prodotto
   *
   * Effettua una richiesta POST a /api/commands/products tramite il GatewayBff.
   * Il BFF inoltra la richiesta al ProductService.
   * @param product - Dati del prodotto da creare (senza ID)
   * @returns Observable<Product> - Il prodotto creato con ID assegnato dal server
   */
  createProduct(product: CreateProductRequest): Observable<Product> {
    return this.http.post<Product>(`${this.commandApiUrl}/products`, product)
      .pipe(
        tap(newProduct => console.log('Prodotto creato:', newProduct)),
        catchError(this.handleError)
      );
  }

  /**
   * Aggiorna un prodotto esistente
   *
   * Effettua una richiesta PUT a /api/commands/products/{id} tramite il GatewayBff.
   * Il BFF inoltra la richiesta al ProductService.
   * @param id - ID del prodotto da aggiornare
   * @param product - Nuovi dati del prodotto
   * @returns Observable<Product> - Il prodotto aggiornato
   */
  updateProduct(id: number, product: UpdateProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.commandApiUrl}/products/${id}`, product)
      .pipe(
        tap(updatedProduct => console.log('Prodotto aggiornato:', updatedProduct)),
        catchError(this.handleError)
      );
  }

  /**
   * Elimina un prodotto
   *
   * Effettua una richiesta DELETE a /api/commands/products/{id} tramite il GatewayBff.
   * Il BFF inoltra la richiesta al ProductService.
   * @param id - ID del prodotto da eliminare
   * @returns Observable<void> - Nessun contenuto di ritorno in caso di successo
   */
  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.commandApiUrl}/products/${id}`)
      .pipe(
        tap(() => console.log('Prodotto eliminato con ID:', id)),
        catchError(this.handleError)
      );
  }

  /**
   * Gestisce gli errori HTTP
   *
   * Metodo privato che processa gli errori HTTP e crea messaggi di errore leggibili.
   * Distingue tra errori lato client (rete, CORS, ecc.) e errori lato server (4xx, 5xx).
   *
   * @param error - Oggetto di errore HTTP ricevuto
   * @returns Observable<never> - Observable che emette un errore
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'Si è verificato un errore';

    if (error.error instanceof ErrorEvent) {
      // Errore lato client (rete, CORS, ecc.)
      errorMessage = `Errore: ${error.error.message}`;
    } else {
      // Errore lato server (codici HTTP 4xx, 5xx)
      errorMessage = `Codice Errore: ${error.status}\nMessaggio: ${error.message}`;
    }

    console.error(errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}
