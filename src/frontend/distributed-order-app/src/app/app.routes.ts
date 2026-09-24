import { Routes } from '@angular/router';
import { ProductsComponent } from './components/products/products';
import { ProductFormComponent } from './components/product-form/product-form';
import { InventoryComponent } from './components/inventory/inventory';
import { OrdersComponent } from './components/orders/orders';
import { CreateOrderComponent } from './components/create-order/create-order';
import { NotFoundComponent } from './components/not-found/not-found';

/**
 * Application Routes configuration
 *
 * Defines all the routes (URL paths) of the application and the components
 * associated with each route. Angular uses this configuration to navigate
 * between the application's different pages without reloading the page (SPA).
 *
 * Available routes:
 * 1. '' (root) -> Redirects to /products
 * 2. /products -> List of all products
 * 3. /products/new -> Form to create a new product
 * 4. /products/edit/:id -> Form to edit an existing product
 * 5. /inventory -> Product inventory management
 * 6. /orders -> List of orders
 * 7. /create-order -> Create a new order
 * 8. ** (any other path) -> Redirects to /products (404 handler)
 */
export const routes: Routes = [
  {
    path: '',                        // Root route (homepage)
    redirectTo: '/products',         // Redirect to the product list
    pathMatch: 'full'                // Must match the empty path exactly
  },
  {
    path: 'products',                // Route for the product list
    component: ProductsComponent,    // Component to render
    title: 'Products'                // Page title (shown in the browser tab)
  },
  {
    path: 'products/new',            // Route to create a new product
    component: ProductFormComponent, // Uses the same form component
    title: 'Add Product'             // Page title
  },
  {
    path: 'products/:id/edit',       // Route to edit a product (:id is a dynamic parameter)
    component: ProductFormComponent, // Uses the same form component (edit mode)
    title: 'Edit Product'            // Page title
  },
  {
    path: 'inventory',               // Route for inventory management
    component: InventoryComponent,   // Component to manage inventory
    title: 'Inventory Management'    // Page title
  },
  {
    path: 'orders',                  // Route for the order list
    component: OrdersComponent,      // Component to display orders
    title: 'Orders'                  // Page title
  },
  {
    path: 'create-order',            // Route to create a new order
    component: CreateOrderComponent, // Component to create orders
    title: 'Create Order'            // Page title
  },
  {
    path: 'not-found',               // Route for the 404 page
    component: NotFoundComponent,    // Component to handle 404 errors
    title: 'Page Not Found'          // Page title
  },
  {
    path: '**',                      // Wildcard route: matches any path not defined above
    component: NotFoundComponent     // Show the 404 page instead of redirecting
  }
];
