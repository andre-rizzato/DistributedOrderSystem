import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap, map } from 'rxjs/operators';
import { Product, CreateProductRequest, UpdateProductRequest, CatalogItem } from '../models/product';

/**
 * ProductService
 *
 * Angular service that handles all HTTP operations related to products.
 * This service is a singleton (providedIn: 'root') shared across the whole application.
 *
 * Main features:
 * - Fetch all products from the catalog (with inventory information)
 * - Fetch a single product by ID
 * - Create a new product
 * - Update an existing product
 * - Delete a product
 * - Centralized HTTP error handling
 *
 * All operations talk to the GatewayBff (Backend for Frontend)
 * running on localhost:5189, which acts as a mediator between the frontend
 * and the backend microservices (ProductService, InventoryService, etc.).
 */
@Injectable({
  providedIn: 'root',
})
export class ProductService {
  // Base URL of the GatewayBff for queries (reads)
  private readonly queryApiUrl = 'http://localhost:5189/api/queries';
  // Base URL of the GatewayBff for commands (writes)
  private readonly commandApiUrl = 'http://localhost:5189/api/commands';

  /**
   * Service constructor
   * @param http - Angular's HTTP client for making HTTP requests
   */
  constructor(private http: HttpClient) { }

  /**
   * Fetches all products from the catalog with inventory information
   *
   * Makes a GET request to /api/queries/catalog through the GatewayBff.
   * The BFF aggregates data from ProductService and InventoryService.
   *
   * @returns Observable<CatalogItem[]> - Array of catalog items with available quantity
   */
  getAllProducts(): Observable<CatalogItem[]> {
    return this.http.get<CatalogItem[]>(`${this.queryApiUrl}/catalog`)
      .pipe(
        tap(products => console.log('Catalog products fetched:', products)),
        catchError(this.handleError)
      );
  }

  /**
   * Fetches a single product by its ID
   *
   * Makes a GET request to /api/queries/catalog/{id} through the GatewayBff.
   * @param id - ID of the product to fetch
   * @returns Observable<CatalogItem> - The requested product with inventory data
   */
  getProductById(id: number): Observable<CatalogItem> {
    return this.http.get<CatalogItem>(`${this.queryApiUrl}/catalog/${id}`)
      .pipe(
        tap(product => console.log('Product fetched:', product)),
        catchError(this.handleError)
      );
  }

  /**
   * Creates a new product
   *
   * Makes a POST request to /api/commands/products through the GatewayBff.
   * The BFF forwards the request to ProductService.
   * @param product - Product data to create (without ID)
   * @returns Observable<Product> - The created product with ID assigned by the server
   */
  createProduct(product: CreateProductRequest): Observable<Product> {
    return this.http.post<Product>(`${this.commandApiUrl}/products`, product)
      .pipe(
        tap(newProduct => console.log('Product created:', newProduct)),
        catchError(this.handleError)
      );
  }

  /**
   * Updates an existing product
   *
   * Makes a PUT request to /api/commands/products/{id} through the GatewayBff.
   * The BFF forwards the request to ProductService.
   * @param id - ID of the product to update
   * @param product - New product data
   * @returns Observable<Product> - The updated product
   */
  updateProduct(id: number, product: UpdateProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.commandApiUrl}/products/${id}`, product)
      .pipe(
        tap(updatedProduct => console.log('Product updated:', updatedProduct)),
        catchError(this.handleError)
      );
  }

  /**
   * Deletes a product
   *
   * Makes a DELETE request to /api/commands/products/{id} through the GatewayBff.
   * The BFF forwards the request to ProductService.
   * @param id - ID of the product to delete
   * @returns Observable<void> - No return content on success
   */
  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.commandApiUrl}/products/${id}`)
      .pipe(
        tap(() => console.log('Product deleted with ID:', id)),
        catchError(this.handleError)
      );
  }

  /**
   * Handles HTTP errors
   *
   * Private method that processes HTTP errors and builds readable error messages.
   * Distinguishes between client-side errors (network, CORS, etc.) and server-side errors (4xx, 5xx).
   *
   * @param error - The received HTTP error object
   * @returns Observable<never> - Observable that emits an error
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An error occurred';

    if (error.error instanceof ErrorEvent) {
      // Client-side error (network, CORS, etc.)
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Server-side error (HTTP status codes 4xx, 5xx)
      errorMessage = `Error code: ${error.status}\nMessage: ${error.message}`;
    }

    console.error(errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}
