export interface Product {
  id: number;
  name: string;
  price: number;
  description: string;
  isActive: boolean;
}

export interface CreateProductRequest {
  name: string;
  price: number;
  description: string;
}

export interface UpdateProductRequest {
  name: string;
  price: number;
  description: string;
  isActive: boolean;
}
