import { Routes } from '@angular/router';
import { ProductsComponent } from './components/products/products';
import { ProductFormComponent } from './components/product-form/product-form';
import { InventoryComponent } from './components/inventory/inventory';
import { OrdersComponent } from './components/orders/orders';
import { CreateOrderComponent } from './components/create-order/create-order';
import { NotFoundComponent } from './components/not-found/not-found';

/**
 * Configurazione delle Routes dell'applicazione
 *
 * Definisce tutte le routes (percorsi URL) dell'applicazione e i componenti
 * associati a ciascuna route. Angular usa questa configurazione per navigare
 * tra le diverse pagine dell'applicazione senza ricaricare la pagina (SPA).
 *
 * Routes disponibili:
 * 1. '' (root) -> Reindirizza a /products
 * 2. /products -> Lista di tutti i prodotti
 * 3. /products/new -> Form per creare un nuovo prodotto
 * 4. /products/edit/:id -> Form per modificare un prodotto esistente
 * 5. /inventory -> Gestione inventario prodotti
 * 6. /orders -> Lista degli ordini
 * 7. /create-order -> Creazione nuovo ordine
 * 8. ** (qualsiasi altro percorso) -> Reindirizza a /products (404 handler)
 */
export const routes: Routes = [
  {
    path: '',                        // Route root (homepage)
    redirectTo: '/products',         // Reindirizza alla lista prodotti
    pathMatch: 'full'                // Deve corrispondere esattamente al path vuoto
  },
  {
    path: 'products',                // Route per la lista prodotti
    component: ProductsComponent,    // Componente da renderizzare
    title: 'Products'                // Titolo della pagina (mostrato nel tab del browser)
  },
  {
    path: 'products/new',            // Route per creare un nuovo prodotto
    component: ProductFormComponent, // Usa lo stesso form component
    title: 'Add Product'             // Titolo della pagina
  },
  {
    path: 'products/:id/edit',       // Route per modificare un prodotto (:id è un parametro dinamico)
    component: ProductFormComponent, // Usa lo stesso form component (modalità modifica)
    title: 'Edit Product'            // Titolo della pagina
  },
  {
    path: 'inventory',               // Route per la gestione inventario
    component: InventoryComponent,   // Componente per gestire l'inventario
    title: 'Inventory Management'    // Titolo della pagina
  },
  {
    path: 'orders',                  // Route per la lista ordini
    component: OrdersComponent,      // Componente per visualizzare gli ordini
    title: 'Orders'                  // Titolo della pagina
  },
  {
    path: 'create-order',            // Route per creare un nuovo ordine
    component: CreateOrderComponent, // Componente per creare ordini
    title: 'Create Order'            // Titolo della pagina
  },
  {
    path: 'not-found',               // Route per pagina 404
    component: NotFoundComponent,    // Componente per gestire errori 404
    title: 'Page Not Found'          // Titolo della pagina
  },
  {
    path: '**',                      // Wildcard route: corrisponde a qualsiasi percorso non definito sopra
    component: NotFoundComponent     // Mostra la pagina 404 invece di reindirizzare
  }
];
