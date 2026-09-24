import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductService } from '../../services/product';
import { Product, CreateProductRequest, UpdateProductRequest } from '../../models/product';

/**
 * ProductFormComponent
 *
 * Form component for creating and editing products.
 *
 * Features:
 * - Creates new products (create mode)
 * - Edits existing products (edit mode)
 * - Full form field validation
 * - Custom error messages for each field
 * - Handles loading and error states
 * - Automatic navigation back to the product list after saving
 *
 * The component uses Angular Reactive Forms for form management,
 * which offers more robust validation and more structured state handling
 * than Template-Driven Forms.
 *
 * Modes:
 * - If the URL contains a numeric ID: EDIT mode (loads the existing product)
 * - If the URL contains 'new': CREATE mode (empty form)
 */
@Component({
  selector: 'app-product-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './product-form.html',
  styleUrl: './product-form.scss'
})
export class ProductFormComponent implements OnInit {
  // FormGroup that manages all form fields and their validation
  productForm: FormGroup;

  // Signal indicating whether we're in edit mode (true) or create mode (false)
  isEdit = signal(false);

  // Signal holding the ID of the product being edited (null if creating)
  productId = signal<number | null>(null);

  // Signal for the loading state
  loading = signal(false);

  // Signal for error messages
  error = signal<string | null>(null);

  /**
   * Constructor
   * @param fb - FormBuilder for building the reactive form
   * @param productService - Service for product operations
   * @param route - ActivatedRoute for reading URL parameters
   * @param router - Router for programmatic navigation
   */
  constructor(
    private fb: FormBuilder,
    private productService: ProductService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    // Initializes the form with validators
    this.productForm = this.fb.group({
      name: ['', [
        Validators.required,           // Required field
        Validators.minLength(1),       // Minimum 1 character
        Validators.maxLength(100)      // Maximum 100 characters
      ]],
      price: [0, [
        Validators.required,           // Required field
        Validators.min(0.01)           // Must be greater than 0
      ]],
      description: ['', [
        Validators.maxLength(500)      // Maximum 500 characters
      ]],
      isActive: [true]                 // Default: active
    });
  }

  /**
   * Lifecycle hook run when the component is initialized
   * Determines whether we're in edit or create mode based on the URL
   */
  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    if (id && id !== 'new') {
      // EDIT mode: numeric ID present in the URL
      this.productId.set(parseInt(id));
      this.isEdit.set(true);
      this.loadProduct();
    }
    // Otherwise: CREATE mode (form stays empty)
  }

  /**
   * Loads the data of the product being edited
   * Populates the form with the product's existing data
   */
  loadProduct(): void {
    const id = this.productId();
    if (!id) return;

    this.loading.set(true);
    this.error.set(null);

    this.productService.getProductById(id).subscribe({
      next: (product) => {
        // Populates the form with the existing product data
        this.productForm.patchValue({
          name: product.name,
          price: product.price,
          description: product.description ?? '',
          isActive: product.isActive
        });
        this.loading.set(false);
      },
      error: (error) => {
        this.error.set('Unable to load the product: ' + error.message);
        this.loading.set(false);
        console.error('Error loading product:', error);
      }
    });
  }

  /**
   * Handles the form submit
   * Validates the data and calls the appropriate service (create or update)
   */
  onSubmit(): void {
    if (this.productForm.invalid) {
      // If the form is invalid, mark all fields as "touched"
      // to display all validation errors
      this.markAllFieldsAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const formValue = this.productForm.value;

    if (this.isEdit()) {
      // EDIT MODE: Update the existing product
      const updateRequest: UpdateProductRequest = {
        name: formValue.name,
        price: formValue.price,
        description: formValue.description,
        isActive: formValue.isActive
      };

      this.productService.updateProduct(this.productId()!, updateRequest).subscribe({
        next: (product) => {
          this.loading.set(false);
          // Navigates back to the product list on success
          this.router.navigate(['/products']);
        },
        error: (error) => {
          this.error.set('Unable to update the product: ' + error.message);
          this.loading.set(false);
          console.error('Error updating product:', error);
        }
      });
    } else {
      // CREATE MODE: Create a new product
      const createRequest: CreateProductRequest = {
        name: formValue.name,
        price: formValue.price,
        description: formValue.description
      };

      this.productService.createProduct(createRequest).subscribe({
        next: (product) => {
          this.loading.set(false);
          // Navigates back to the product list on success
          this.router.navigate(['/products']);
        },
        error: (error) => {
          this.error.set('Unable to create the product: ' + error.message);
          this.loading.set(false);
          console.error('Error creating product:', error);
        }
      });
    }
  }

  /**
   * Handles the click on the Cancel button
   * Returns to the product list without saving
   */
  onCancel(): void {
    this.router.navigate(['/products']);
  }

  /**
   * Marks all form fields as "touched"
   * Useful for showing all validation errors on submit
   */
  private markAllFieldsAsTouched(): void {
    Object.keys(this.productForm.controls).forEach(key => {
      this.productForm.get(key)?.markAsTouched();
    });
  }

  /**
   * Gets the error message for a specific field
   *
   * Inspects validation errors and returns a readable message.
   * Supports: required, minlength, maxlength, min
   *
   * @param fieldName - Name of the field to check
   * @returns Error message, or null if the field is valid
   */
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

  /**
   * Converts the technical field name into a display name
   * @param fieldName - Technical field name (e.g. 'name', 'price')
   * @returns Display name (e.g. 'Name', 'Price')
   */
  private getFieldDisplayName(fieldName: string): string {
    const displayNames: { [key: string]: string } = {
      name: 'Name',
      price: 'Price',
      description: 'Description'
    };
    return displayNames[fieldName] || fieldName;
  }

  /**
   * Checks whether a field is invalid and should display its error
   * @param fieldName - Name of the field to check
   * @returns true if the field is invalid and has been touched/modified
   */
  isFieldInvalid(fieldName: string): boolean {
    const field = this.productForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }
}
