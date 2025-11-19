import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { Product, CreateProductRequest, UpdateProductRequest } from '../models/product';

/**
 * ProductService
 *
 * Servizio Angular che gestisce tutte le operazioni HTTP relative ai prodotti.
 * Questo servizio è un singleton (providedIn: 'root') condiviso in tutta l'applicazione.
 *
 * Funzionalità principali:
 * - Recuperare tutti i prodotti dal backend
 * - Recuperare un singolo prodotto per ID
 * - Creare un nuovo prodotto
 * - Aggiornare un prodotto esistente
 * - Eliminare un prodotto
 * - Gestione centralizzata degli errori HTTP
 *
 * Tutte le operazioni comunicano con l'API REST del ProductService backend
 * in esecuzione su localhost:5198.
 */
@Injectable({
  providedIn: 'root',
})
export class ProductService {
  // URL base dell'API del backend per i prodotti
  private readonly apiUrl = 'http://localhost:5198/api/products';

  /**
   * Costruttore del servizio
   * @param http - Client HTTP di Angular per effettuare richieste HTTP
   */
  constructor(private http: HttpClient) { }

  /**
   * Recupera tutti i prodotti dal backend
   *
   * Effettua una richiesta GET a /api/products
   * @returns Observable<Product[]> - Array di prodotti
   */
  getAllProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(this.apiUrl)
      .pipe(
        tap(products => console.log('Prodotti recuperati:', products)),
        catchError(this.handleError)
      );
  }

  /**
   * Recupera un singolo prodotto tramite il suo ID
   *
   * Effettua una richiesta GET a /api/products/{id}
   * @param id - ID del prodotto da recuperare
   * @returns Observable<Product> - Il prodotto richiesto
   */
  getProductById(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`)
      .pipe(
        tap(product => console.log('Prodotto recuperato:', product)),
        catchError(this.handleError)
      );
  }

  /**
   * Crea un nuovo prodotto
   *
   * Effettua una richiesta POST a /api/products con i dati del nuovo prodotto
   * @param product - Dati del prodotto da creare (senza ID)
   * @returns Observable<Product> - Il prodotto creato con ID assegnato dal server
   */
  createProduct(product: CreateProductRequest): Observable<Product> {
    return this.http.post<Product>(this.apiUrl, product)
      .pipe(
        tap(newProduct => console.log('Prodotto creato:', newProduct)),
        catchError(this.handleError)
      );
  }

  /**
   * Aggiorna un prodotto esistente
   *
   * Effettua una richiesta PUT a /api/products/{id} con i dati aggiornati
   * @param id - ID del prodotto da aggiornare
   * @param product - Nuovi dati del prodotto
   * @returns Observable<Product> - Il prodotto aggiornato
   */
  updateProduct(id: number, product: UpdateProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.apiUrl}/${id}`, product)
      .pipe(
        tap(updatedProduct => console.log('Prodotto aggiornato:', updatedProduct)),
        catchError(this.handleError)
      );
  }

  /**
   * Elimina un prodotto
   *
   * Effettua una richiesta DELETE a /api/products/{id}
   * @param id - ID del prodotto da eliminare
   * @returns Observable<void> - Nessun contenuto di ritorno in caso di successo
   */
  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`)
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
