import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProductService } from '../../services/product';
import { Product } from '../../models/product';

/**
 * ProductsComponent
 *
 * Componente principale per la visualizzazione e gestione della lista prodotti.
 *
 * Funzionalità:
 * - Visualizza tutti i prodotti in una lista
 * - Permette di eliminare prodotti con conferma
 * - Permette di attivare/disattivare prodotti (toggle dello stato isActive)
 * - Gestisce gli stati di caricamento ed errore
 * - Fornisce link per modificare prodotti esistenti e crearne di nuovi
 *
 * Utilizza Angular Signals (signal) per la gestione reattiva dello stato.
 * I signals sono una nuova funzionalità di Angular che permette una gestione
 * più efficiente e performante dello stato rispetto agli Observable tradizionali.
 */
@Component({
  selector: 'app-products',
  imports: [CommonModule, RouterModule],
  templateUrl: './products.html',
  styleUrl: './products.scss'
})
export class ProductsComponent implements OnInit {
  // Signal che contiene l'array di prodotti (reattivo)
  products = signal<Product[]>([]);

  // Signal che indica se il componente sta caricando dati
  loading = signal(false);

  // Signal che contiene eventuali messaggi di errore
  error = signal<string | null>(null);

  /**
   * Costruttore
   * @param productService - Servizio iniettato per le operazioni sui prodotti
   */
  constructor(private productService: ProductService) {}

  /**
   * Lifecycle hook eseguito all'inizializzazione del componente
   * Carica automaticamente la lista dei prodotti quando il componente viene creato
   */
  ngOnInit(): void {
    this.loadProducts();
  }

  /**
   * Carica tutti i prodotti dal backend
   *
   * Imposta lo stato di caricamento, effettua la richiesta HTTP tramite il servizio,
   * e aggiorna i signals in base al risultato (successo o errore).
   */
  loadProducts(): void {
    this.loading.set(true);
    this.error.set(null);

    this.productService.getAllProducts().subscribe({
      next: (products) => {
        this.products.set(products);
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
   * Elimina un prodotto
   *
   * Mostra una finestra di conferma prima di procedere con l'eliminazione.
   * Se confermato, elimina il prodotto dal backend e lo rimuove dalla lista locale.
   *
   * @param id - ID del prodotto da eliminare
   */
  deleteProduct(id: number): void {
    if (confirm('Sei sicuro di voler eliminare questo prodotto?')) {
      this.productService.deleteProduct(id).subscribe({
        next: () => {
          // Rimuove il prodotto eliminato dalla lista locale
          const currentProducts = this.products();
          this.products.set(currentProducts.filter(p => p.id !== id));
        },
        error: (error) => {
          this.error.set('Impossibile eliminare il prodotto: ' + error.message);
          console.error('Errore nell\'eliminazione del prodotto:', error);
        }
      });
    }
  }

  /**
   * Attiva/Disattiva lo stato di un prodotto (toggle isActive)
   *
   * Inverte lo stato isActive del prodotto e aggiorna il backend.
   * Aggiorna anche la lista locale con il prodotto modificato.
   *
   * @param product - Prodotto di cui modificare lo stato
   */
  toggleProductStatus(product: Product): void {
    // Crea una richiesta di aggiornamento con lo stato isActive invertito
    const updateRequest = {
      name: product.name,
      price: product.price,
      description: product.description,
      isActive: !product.isActive
    };

    this.productService.updateProduct(product.id, updateRequest).subscribe({
      next: (updatedProduct) => {
        // Aggiorna il prodotto nella lista locale
        const currentProducts = this.products();
        const index = currentProducts.findIndex(p => p.id === product.id);
        if (index !== -1) {
          currentProducts[index] = updatedProduct;
          this.products.set([...currentProducts]); // Crea un nuovo array per triggare il signal
        }
      },
      error: (error) => {
        this.error.set('Impossibile aggiornare il prodotto: ' + error.message);
        console.error('Errore nell\'aggiornamento del prodotto:', error);
      }
    });
  }
}
