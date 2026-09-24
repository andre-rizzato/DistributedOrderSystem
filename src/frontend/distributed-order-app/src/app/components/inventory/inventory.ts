import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../services/product';
import { InventoryService } from '../../services/inventory';
import { CatalogItem } from '../../models/product';

/**
 * InventoryComponent
 *
 * Component for managing product inventory.
 *
 * Features:
 * - Displays all products with their inventory quantities
 * - Allows adding/removing quantity from inventory
 * - Allows setting a specific quantity
 * - Handles loading and error states
 */
@Component({
  selector: 'app-inventory',
  imports: [CommonModule, FormsModule],
  templateUrl: './inventory.html',
  styleUrl: './inventory.scss'
})
export class InventoryComponent implements OnInit {
  // Signal holding the array of catalog items (products + inventory)
  products = signal<CatalogItem[]>([]);

  // Signal indicating whether the component is loading data
  loading = signal(false);

  // Signal holding any error messages
  error = signal<string | null>(null);

  // Signal for success messages
  success = signal<string | null>(null);

  // Maps holding the input quantities for each product
  adjustQuantities = new Map<number, number>();
  setQuantities = new Map<number, number>();

  /**
   * Constructor
   * @param productService - Service for product operations
   * @param inventoryService - Service for inventory operations
   */
  constructor(
    private productService: ProductService,
    private inventoryService: InventoryService
  ) {}

  /**
   * Lifecycle hook run when the component is initialized
   */
  ngOnInit(): void {
    this.loadProducts();
  }

  /**
   * Loads all products along with their inventory data
   */
  loadProducts(): void {
    this.loading.set(true);
    this.error.set(null);
    this.success.set(null);

    this.productService.getAllProducts().subscribe({
      next: (products) => {
        this.products.set(products);
        // Initializes the quantities for each product
        products.forEach(p => {
          this.adjustQuantities.set(p.productId, 0);
          this.setQuantities.set(p.productId, p.availableQuantity);
        });
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
   * Adjusts inventory (adds or removes quantity)
   *
   * @param productId - Product ID
   */
  adjustInventory(productId: number): void {
    const delta = this.adjustQuantities.get(productId) || 0;

    if (delta === 0) {
      this.error.set('Quantity must be different from zero');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.success.set(null);

    this.inventoryService.adjustInventory({ productId, delta }).subscribe({
      next: () => {
        this.success.set(`Inventory ${delta > 0 ? 'increased' : 'decreased'} by ${Math.abs(delta)} unit(s)`);
        this.adjustQuantities.set(productId, 0);
        this.loadProducts();
      },
      error: (error) => {
        this.error.set('Unable to adjust inventory: ' + error.message);
        this.loading.set(false);
        console.error('Error adjusting inventory:', error);
      }
    });
  }

  /**
   * Sets inventory to a specific value
   *
   * @param productId - Product ID
   */
  setInventory(productId: number): void {
    const quantity = this.setQuantities.get(productId);

    if (quantity === undefined || quantity < 0) {
      this.error.set('Quantity must be a positive number');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.success.set(null);

    this.inventoryService.setInventory({ productId, quantity }).subscribe({
      next: () => {
        this.success.set(`Inventory set to ${quantity} unit(s)`);
        this.loadProducts();
      },
      error: (error) => {
        this.error.set('Unable to set inventory: ' + error.message);
        this.loading.set(false);
        console.error('Error setting inventory:', error);
      }
    });
  }

  /**
   * Gets the adjustment value for a product
   */
  getAdjustValue(productId: number): number {
    return this.adjustQuantities.get(productId) || 0;
  }

  /**
   * Sets the adjustment value for a product
   */
  setAdjustValue(productId: number, value: string): void {
    const numValue = parseInt(value) || 0;
    this.adjustQuantities.set(productId, numValue);
  }

  /**
   * Gets the set value for a product
   */
  getSetValue(productId: number): number {
    return this.setQuantities.get(productId) || 0;
  }

  /**
   * Sets the set value for a product
   */
  setSetValue(productId: number, value: string): void {
    const numValue = parseInt(value) || 0;
    this.setQuantities.set(productId, numValue);
  }
}
