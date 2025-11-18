import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProductService } from '../../services/product';
import { Product } from '../../models/product';

@Component({
  selector: 'app-products',
  imports: [CommonModule, RouterModule],
  templateUrl: './products.html',
  styleUrl: './products.scss'
})
export class ProductsComponent implements OnInit {
  products = signal<Product[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  constructor(private productService: ProductService) {}

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading.set(true);
    this.error.set(null);
    
    this.productService.getAllProducts().subscribe({
      next: (products) => {
        this.products.set(products);
        this.loading.set(false);
      },
      error: (error) => {
        this.error.set('Failed to load products: ' + error.message);
        this.loading.set(false);
        console.error('Error loading products:', error);
      }
    });
  }

  deleteProduct(id: number): void {
    if (confirm('Are you sure you want to delete this product?')) {
      this.productService.deleteProduct(id).subscribe({
        next: () => {
          // Remove the deleted product from the list
          const currentProducts = this.products();
          this.products.set(currentProducts.filter(p => p.id !== id));
        },
        error: (error) => {
          this.error.set('Failed to delete product: ' + error.message);
          console.error('Error deleting product:', error);
        }
      });
    }
  }

  toggleProductStatus(product: Product): void {
    const updateRequest = {
      name: product.name,
      price: product.price,
      description: product.description,
      isActive: !product.isActive
    };

    this.productService.updateProduct(product.id, updateRequest).subscribe({
      next: (updatedProduct) => {
        // Update the product in the list
        const currentProducts = this.products();
        const index = currentProducts.findIndex(p => p.id === product.id);
        if (index !== -1) {
          currentProducts[index] = updatedProduct;
          this.products.set([...currentProducts]);
        }
      },
      error: (error) => {
        this.error.set('Failed to update product: ' + error.message);
        console.error('Error updating product:', error);
      }
    });
  }
}
