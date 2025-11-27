export interface OrderItem {
  productId: number;
  quantity: number;
  unitPrice: number;
}

export interface CreateOrderRequest {
  items: OrderItem[];
}

export interface CreateOrderResponse {
  orderId: number;
  status: string;
  total: number;
}

export interface OrderItemDetail {
  id: number;
  productId: number;
  quantity: number;
  unitPrice: number;
}

export interface Order {
  id: number;
  createdAt: string;
  status: string;
  total: number;
  items: OrderItemDetail[];
}
