import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../services/product';
import { InventoryService } from '../../services/inventory';
import { CatalogItem } from '../../models/product';

/**
 * InventoryComponent
 *
 * Componente per la gestione dell'inventario prodotti.
 *
 * Funzionalità:
 * - Visualizza tutti i prodotti con le loro quantità in inventario
 * - Permette di aggiungere/rimuovere quantità dall'inventario
 * - Permette di impostare una quantità specifica
 * - Gestisce gli stati di caricamento ed errore
 */
@Component({
  selector: 'app-inventory',
  imports: [CommonModule, FormsModule],
  templateUrl: './inventory.html',
  styleUrl: './inventory.scss'
})
export class InventoryComponent implements OnInit {
  // Signal che contiene l'array di elementi del catalogo (prodotti + inventario)
  products = signal<CatalogItem[]>([]);

  // Signal che indica se il componente sta caricando dati
  loading = signal(false);

  // Signal che contiene eventuali messaggi di errore
  error = signal<string | null>(null);

  // Signal per messaggi di successo
  success = signal<string | null>(null);

  // Map per gestire le quantità di input per ciascun prodotto
  adjustQuantities = new Map<number, number>();
  setQuantities = new Map<number, number>();

  /**
   * Costruttore
   * @param productService - Servizio per operazioni sui prodotti
   * @param inventoryService - Servizio per operazioni sull'inventario
   */
  constructor(
    private productService: ProductService,
    private inventoryService: InventoryService
  ) {}

  /**
   * Lifecycle hook eseguito all'inizializzazione del componente
   */
  ngOnInit(): void {
    this.loadProducts();
  }

  /**
   * Carica tutti i prodotti con i dati dell'inventario
   */
  loadProducts(): void {
    this.loading.set(true);
    this.error.set(null);
    this.success.set(null);

    this.productService.getAllProducts().subscribe({
      next: (products) => {
        this.products.set(products);
        // Inizializza le quantità per ogni prodotto
        products.forEach(p => {
          this.adjustQuantities.set(p.productId, 0);
          this.setQuantities.set(p.productId, p.availableQuantity);
        });
        this.loading.set(false);
      },
      error: (error) => {
        this.error.set('Impossibile caricare i prodotti: ' + error.message);
        this.loading.set(false);
        console.error('Errore nel caricamento dei prodotti:', error);
      }
    });
  }

  /**
   * Aggiusta l'inventario (aggiunge o rimuove quantità)
   *
   * @param productId - ID del prodotto
   */
  adjustInventory(productId: number): void {
    const delta = this.adjustQuantities.get(productId) || 0;

    if (delta === 0) {
      this.error.set('La quantità deve essere diversa da zero');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.success.set(null);

    this.inventoryService.adjustInventory({ productId, delta }).subscribe({
      next: () => {
        this.success.set(`Inventario ${delta > 0 ? 'aumentato' : 'diminuito'} di ${Math.abs(delta)} unità`);
        this.adjustQuantities.set(productId, 0);
        this.loadProducts();
      },
      error: (error) => {
        this.error.set('Impossibile aggiustare l\'inventario: ' + error.message);
        this.loading.set(false);
        console.error('Errore nell\'aggiustamento dell\'inventario:', error);
      }
    });
  }

  /**
   * Imposta l'inventario ad un valore specifico
   *
   * @param productId - ID del prodotto
   */
  setInventory(productId: number): void {
    const quantity = this.setQuantities.get(productId);

    if (quantity === undefined || quantity < 0) {
      this.error.set('La quantità deve essere un numero positivo');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.success.set(null);

    this.inventoryService.setInventory({ productId, quantity }).subscribe({
      next: () => {
        this.success.set(`Inventario impostato a ${quantity} unità`);
        this.loadProducts();
      },
      error: (error) => {
        this.error.set('Impossibile impostare l\'inventario: ' + error.message);
        this.loading.set(false);
        console.error('Errore nell\'impostazione dell\'inventario:', error);
      }
    });
  }

  /**
   * Ottiene il valore di aggiustamento per un prodotto
   */
  getAdjustValue(productId: number): number {
    return this.adjustQuantities.get(productId) || 0;
  }

  /**
   * Imposta il valore di aggiustamento per un prodotto
   */
  setAdjustValue(productId: number, value: string): void {
    const numValue = parseInt(value) || 0;
    this.adjustQuantities.set(productId, numValue);
  }

  /**
   * Ottiene il valore di impostazione per un prodotto
   */
  getSetValue(productId: number): number {
    return this.setQuantities.get(productId) || 0;
  }

  /**
   * Imposta il valore di impostazione per un prodotto
   */
  setSetValue(productId: number, value: string): void {
    const numValue = parseInt(value) || 0;
    this.setQuantities.set(productId, numValue);
  }
}
