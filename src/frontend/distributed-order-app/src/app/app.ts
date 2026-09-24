import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterModule } from '@angular/router';

/**
 * App - Root component of the application
 *
 * This is the main (root) component of the entire Angular application.
 * It's loaded first and contains the router outlet that handles navigation
 * between the application's different pages/components.
 *
 * Responsibilities:
 * - Provides the mount point for the Angular application
 * - Contains the <router-outlet> that renders components based on the active route
 * - Defines the app's main template and global styles
 *
 * The RouterOutlet acts like a "placeholder" where Angular dynamically inserts
 * the components corresponding to the current route (e.g. ProductsComponent, ProductFormComponent).
 */
@Component({
  selector: 'app-root',              // Selector used in index.html
  imports: [RouterOutlet, RouterModule], // Modules required for routing
  templateUrl: './app.html',         // Component's HTML template
  styleUrl: './app.scss'             // Component's SCSS styles
})
export class App {
  // Signal holding the application's title
  // Can be used in the template to display the app's name
  protected readonly title = signal('distributed-order-app');
}
