import { Routes } from '@angular/router';
import { ProductsComponent } from './components/products/products';
import { ProductFormComponent } from './components/product-form/product-form';

export const routes: Routes = [
  { 
    path: '', 
    redirectTo: '/products', 
    pathMatch: 'full' 
  },
  { 
    path: 'products', 
    component: ProductsComponent,
    title: 'Products' 
  },
  { 
    path: 'products/new', 
    component: ProductFormComponent,
    title: 'Add Product' 
  },
  { 
    path: 'products/edit/:id', 
    component: ProductFormComponent,
    title: 'Edit Product' 
  },
  {
    path: '**',
    redirectTo: '/products'
  }
];
