import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterLink, Router } from '@angular/router';
import { ProductService } from '../../services/product';
import { NavigationService } from '../../services/navigation';
import { CatalogItem } from '../../models/product';

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
  imports: [CommonModule, RouterModule, RouterLink],
  templateUrl: './products.html',
  styleUrl: './products.scss'
})
export class ProductsComponent implements OnInit {
  // Signal che contiene l'array di elementi del catalogo (prodotti + inventario)
  products = signal<CatalogItem[]>([]);

  // Signal che indica se il componente sta caricando dati
  loading = signal(false);

  // Signal che contiene eventuali messaggi di errore
  error = signal<string | null>(null);

  /**
   * Costruttore
   * @param productService - Servizio iniettato per le operazioni sui prodotti
   * @param navigationService - Servizio per gestire navigazione con error handling
   * @param router - Router di Angular per navigazione programmatica
   */
  constructor(
    private productService: ProductService,
    private navigationService: NavigationService,
    private router: Router
  ) {}

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
          this.products.set(currentProducts.filter(p => p.productId !== id));
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
   * @param product - Elemento del catalogo di cui modificare lo stato
   */
  toggleProductStatus(product: CatalogItem): void {
    // Crea una richiesta di aggiornamento con lo stato isActive invertito
    const updateRequest = {
      name: product.name,
      price: product.price,
      description: product.description ?? '',
      isActive: !product.isActive
    };

    this.productService.updateProduct(product.productId, updateRequest).subscribe({
      next: (updatedProduct) => {
        // Ricarica la lista per ottenere i dati aggiornati dal catalogo
        this.loadProducts();
      },
      error: (error) => {
        this.error.set('Impossibile aggiornare il prodotto: ' + error.message);
        console.error('Errore nell\'aggiornamento del prodotto:', error);
      }
    });
  }

  /**
   * Naviga alla pagina di modifica del prodotto con gestione errori
   *
   * @param productId ID del prodotto da modificare
   */
  async navigateToEdit(productId: number): Promise<void> {
    try {
      const success = await this.navigationService.navigateWithErrorHandling(['/products', productId.toString(), 'edit']);
      if (!success) {
        this.error.set(`Impossibile navigare alla pagina di modifica del prodotto ${productId}`);
      }
    } catch (error) {
      console.error('Navigation error:', error);
      this.error.set('Errore durante la navigazione. Verifica che la pagina esista.');
    }
  }

  /**
   * Naviga alla pagina di creazione nuovo prodotto con gestione errori
   */
  async navigateToCreate(): Promise<void> {
    try {
      const success = await this.navigationService.navigateWithErrorHandling(['/products/new']);
      if (!success) {
        this.error.set('Impossibile navigare alla pagina di creazione prodotto');
      }
    } catch (error) {
      console.error('Navigation error:', error);
      this.error.set('Errore durante la navigazione.');
    }
  }

  /**
   * Verifica che un elemento del catalogo sia valido prima di operazioni critiche
   *
   * @param product Elemento del catalogo da verificare
   * @returns true se l'elemento è valido, false altrimenti
   */
  private isValidProduct(product: CatalogItem): boolean {
    if (!product) return false;
    if (typeof product.productId !== 'number' || product.productId <= 0) return false;
    if (!product.name || product.name.trim().length === 0) return false;
    return true;
  }
}
