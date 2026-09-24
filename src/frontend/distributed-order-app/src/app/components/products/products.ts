import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterLink, Router } from '@angular/router';
import { ProductService } from '../../services/product';
import { NavigationService } from '../../services/navigation';
import { CatalogItem } from '../../models/product';

/**
 * ProductsComponent
 *
 * Main component for displaying and managing the product list.
 *
 * Features:
 * - Displays all products in a list
 * - Allows deleting products with confirmation
 * - Allows activating/deactivating products (toggling isActive)
 * - Handles loading and error states
 * - Provides links to edit existing products and create new ones
 *
 * Uses Angular Signals (signal) for reactive state management.
 * Signals are a newer Angular feature that enables more efficient
 * and performant state management compared to traditional Observables.
 */
@Component({
  selector: 'app-products',
  imports: [CommonModule, RouterModule, RouterLink],
  templateUrl: './products.html',
  styleUrl: './products.scss'
})
export class ProductsComponent implements OnInit {
  // Signal holding the array of catalog items (products + inventory)
  products = signal<CatalogItem[]>([]);

  // Signal indicating whether the component is loading data
  loading = signal(false);

  // Signal holding any error messages
  error = signal<string | null>(null);

  /**
   * Constructor
   * @param productService - Injected service for product operations
   * @param navigationService - Service for navigation with error handling
   * @param router - Angular Router for programmatic navigation
   */
  constructor(
    private productService: ProductService,
    private navigationService: NavigationService,
    private router: Router
  ) {}

  /**
   * Lifecycle hook run when the component is initialized
   * Automatically loads the product list when the component is created
   */
  ngOnInit(): void {
    this.loadProducts();
  }

  /**
   * Loads all products from the backend
   *
   * Sets the loading state, makes the HTTP request through the service,
   * and updates the signals based on the result (success or error).
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
        this.error.set('Unable to load products: ' + error.message);
        this.loading.set(false);
        console.error('Error loading products:', error);
      }
    });
  }

  /**
   * Deletes a product
   *
   * Shows a confirmation dialog before proceeding with deletion.
   * If confirmed, deletes the product from the backend and removes it from the local list.
   *
   * @param id - ID of the product to delete
   */
  deleteProduct(id: number): void {
    if (confirm('Are you sure you want to delete this product?')) {
      this.productService.deleteProduct(id).subscribe({
        next: () => {
          // Removes the deleted product from the local list
          const currentProducts = this.products();
          this.products.set(currentProducts.filter(p => p.productId !== id));
        },
        error: (error) => {
          this.error.set('Unable to delete the product: ' + error.message);
          console.error('Error deleting product:', error);
        }
      });
    }
  }

  /**
   * Activates/deactivates a product's status (toggles isActive)
   *
   * Flips the product's isActive status and updates the backend.
   * Also refreshes the local list with the modified product.
   *
   * @param product - Catalog item whose status to change
   */
  toggleProductStatus(product: CatalogItem): void {
    // Builds an update request with the isActive status flipped
    const updateRequest = {
      name: product.name,
      price: product.price,
      description: product.description ?? '',
      isActive: !product.isActive
    };

    this.productService.updateProduct(product.productId, updateRequest).subscribe({
      next: (updatedProduct) => {
        // Reloads the list to get the updated catalog data
        this.loadProducts();
      },
      error: (error) => {
        this.error.set('Unable to update the product: ' + error.message);
        console.error('Error updating product:', error);
      }
    });
  }

  /**
   * Navigates to the product edit page with error handling
   *
   * @param productId ID of the product to edit
   */
  async navigateToEdit(productId: number): Promise<void> {
    try {
      const success = await this.navigationService.navigateWithErrorHandling(['/products', productId.toString(), 'edit']);
      if (!success) {
        this.error.set(`Unable to navigate to the edit page for product ${productId}`);
      }
    } catch (error) {
      console.error('Navigation error:', error);
      this.error.set('An error occurred during navigation. Check that the page exists.');
    }
  }

  /**
   * Navigates to the new product creation page with error handling
   */
  async navigateToCreate(): Promise<void> {
    try {
      const success = await this.navigationService.navigateWithErrorHandling(['/products/new']);
      if (!success) {
        this.error.set('Unable to navigate to the product creation page');
      }
    } catch (error) {
      console.error('Navigation error:', error);
      this.error.set('An error occurred during navigation.');
    }
  }

  /**
   * Verifies that a catalog item is valid before critical operations
   *
   * @param product Catalog item to verify
   * @returns true if the item is valid, false otherwise
   */
  private isValidProduct(product: CatalogItem): boolean {
    if (!product) return false;
    if (typeof product.productId !== 'number' || product.productId <= 0) return false;
    if (!product.name || product.name.trim().length === 0) return false;
    return true;
  }
}
