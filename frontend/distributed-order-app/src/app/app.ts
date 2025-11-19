import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterModule } from '@angular/router';

/**
 * App - Componente Root dell'applicazione
 *
 * Questo è il componente principale (root) dell'intera applicazione Angular.
 * Viene caricato per primo e contiene il router outlet che gestisce la navigazione
 * tra le diverse pagine/componenti dell'applicazione.
 *
 * Funzionalità:
 * - Fornisce il punto di mount per l'applicazione Angular
 * - Contiene il <router-outlet> che renderizza i componenti in base alla route attiva
 * - Definisce il template principale e gli stili globali dell'app
 *
 * Il RouterOutlet è come un "segnaposto" dove Angular inserisce dinamicamente
 * i componenti corrispondenti alla route corrente (es. ProductsComponent, ProductFormComponent).
 */
@Component({
  selector: 'app-root',              // Selettore usato nel index.html
  imports: [RouterOutlet, RouterModule], // Moduli necessari per il routing
  templateUrl: './app.html',         // Template HTML del componente
  styleUrl: './app.scss'             // Stili SCSS del componente
})
export class App {
  // Signal che contiene il titolo dell'applicazione
  // Può essere utilizzato nel template per visualizzare il nome dell'app
  protected readonly title = signal('distributed-order-app');
}
