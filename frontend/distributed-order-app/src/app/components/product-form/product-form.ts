import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductService } from '../../services/product';
import { Product, CreateProductRequest, UpdateProductRequest } from '../../models/product';

@Component({
  selector: 'app-product-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './product-form.html',
  styleUrl: './product-form.scss'
})
export class ProductFormComponent implements OnInit {
  productForm: FormGroup;
  isEdit = signal(false);
  productId = signal<number | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  constructor(
    private fb: FormBuilder,
    private productService: ProductService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.productForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(100)]],
      price: [0, [Validators.required, Validators.min(0.01)]],
      description: ['', [Validators.maxLength(500)]],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    if (id && id !== 'new') {
      this.productId.set(parseInt(id));
      this.isEdit.set(true);
      this.loadProduct();
    }
  }

  loadProduct(): void {
    const id = this.productId();
    if (!id) return;

    this.loading.set(true);
    this.error.set(null);

    this.productService.getProductById(id).subscribe({
      next: (product) => {
        this.productForm.patchValue({
          name: product.name,
          price: product.price,
          description: product.description,
          isActive: product.isActive
        });
        this.loading.set(false);
      },
      error: (error) => {
        this.error.set('Failed to load product: ' + error.message);
        this.loading.set(false);
        console.error('Error loading product:', error);
      }
    });
  }

  onSubmit(): void {
    if (this.productForm.invalid) {
      this.markAllFieldsAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const formValue = this.productForm.value;

    if (this.isEdit()) {
      // Update existing product
      const updateRequest: UpdateProductRequest = {
        name: formValue.name,
        price: formValue.price,
        description: formValue.description,
        isActive: formValue.isActive
      };

      this.productService.updateProduct(this.productId()!, updateRequest).subscribe({
        next: (product) => {
          this.loading.set(false);
          this.router.navigate(['/products']);
        },
        error: (error) => {
          this.error.set('Failed to update product: ' + error.message);
          this.loading.set(false);
          console.error('Error updating product:', error);
        }
      });
    } else {
      // Create new product
      const createRequest: CreateProductRequest = {
        name: formValue.name,
        price: formValue.price,
        description: formValue.description
      };

      this.productService.createProduct(createRequest).subscribe({
        next: (product) => {
          this.loading.set(false);
          this.router.navigate(['/products']);
        },
        error: (error) => {
          this.error.set('Failed to create product: ' + error.message);
          this.loading.set(false);
          console.error('Error creating product:', error);
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/products']);
  }

  private markAllFieldsAsTouched(): void {
    Object.keys(this.productForm.controls).forEach(key => {
      this.productForm.get(key)?.markAsTouched();
    });
  }

  // Helper methods for template
  getFieldError(fieldName: string): string | null {
    const field = this.productForm.get(fieldName);
    if (field && field.invalid && (field.dirty || field.touched)) {
      if (field.errors?.['required']) {
        return `${this.getFieldDisplayName(fieldName)} is required`;
      }
      if (field.errors?.['minlength']) {
        return `${this.getFieldDisplayName(fieldName)} must be at least ${field.errors['minlength'].requiredLength} character(s)`;
      }
      if (field.errors?.['maxlength']) {
        return `${this.getFieldDisplayName(fieldName)} cannot exceed ${field.errors['maxlength'].requiredLength} characters`;
      }
      if (field.errors?.['min']) {
        return `${this.getFieldDisplayName(fieldName)} must be greater than ${field.errors['min'].min}`;
      }
    }
    return null;
  }

  private getFieldDisplayName(fieldName: string): string {
    const displayNames: { [key: string]: string } = {
      name: 'Name',
      price: 'Price',
      description: 'Description'
    };
    return displayNames[fieldName] || fieldName;
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.productForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }
}
