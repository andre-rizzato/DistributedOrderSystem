import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { OrderService } from '../../services/order';
import { ProductService } from '../../services/product';
import { CatalogItem } from '../../models/product';
import { OrderItem, CreateOrderRequest } from '../../models/order';

@Component({
  selector: 'app-create-order',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './create-order.html',
  styleUrls: ['./create-order.scss']
})
export class CreateOrderComponent implements OnInit {
  private orderService = inject(OrderService);
  private productService = inject(ProductService);
  private router = inject(Router);

  products = signal<CatalogItem[]>([]);
  selectedItems = signal<Map<number, { product: CatalogItem, quantity: number }>>(new Map());
  loading = signal(false);
  error = signal<string | null>(null);
  submitting = signal(false);

  // Expose Array for template
  Array = Array;  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading.set(true);
    this.error.set(null);

    this.productService.getAllProducts().subscribe({
      next: (products) => {
        this.products.set(products.filter(p => p.isActive));
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading products:', err);
        this.error.set('Impossibile caricare i prodotti');
        this.loading.set(false);
      }
    });
  }

  addItem(product: CatalogItem): void {
    const current = this.selectedItems();
    if (current.has(product.productId)) {
      const item = current.get(product.productId)!;
      if (item.quantity < product.availableQuantity) {
        current.set(product.productId, { product, quantity: item.quantity + 1 });
        this.selectedItems.set(new Map(current));
      }
    } else {
      if (product.availableQuantity > 0) {
        current.set(product.productId, { product, quantity: 1 });
        this.selectedItems.set(new Map(current));
      }
    }
  }

  removeItem(productId: number): void {
    const current = this.selectedItems();
    current.delete(productId);
    this.selectedItems.set(new Map(current));
  }

  updateQuantity(productId: number, quantity: number): void {
    const current = this.selectedItems();
    const item = current.get(productId);
    if (item && quantity > 0 && quantity <= item.product.availableQuantity) {
      current.set(productId, { ...item, quantity });
      this.selectedItems.set(new Map(current));
    }
  }

  getTotal(): number {
    let total = 0;
    this.selectedItems().forEach(item => {
      total += item.product.price * item.quantity;
    });
    return total;
  }

  canSubmit(): boolean {
    return this.selectedItems().size > 0 && !this.submitting();
  }

  submitOrder(): void {
    if (!this.canSubmit()) return;

    this.submitting.set(true);
    this.error.set(null);

    const items: OrderItem[] = Array.from(this.selectedItems().values()).map(item => ({
      productId: item.product.productId,
      quantity: item.quantity,
      unitPrice: item.product.price
    }));

    const request: CreateOrderRequest = { items };

    this.orderService.createOrder(request).subscribe({
      next: (response) => {
        console.log('Order created:', response);
        this.router.navigate(['/orders']);
      },
      error: (err) => {
        console.error('Error creating order:', err);
        this.error.set(err.error?.message || 'Errore durante la creazione dell\'ordine');
        this.submitting.set(false);
      }
    });
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('it-IT', {
      style: 'currency',
      currency: 'EUR'
    }).format(value);
  }
}
