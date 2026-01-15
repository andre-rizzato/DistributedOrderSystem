/**
 * Interfaccia InventoryItem
 *
 * Rappresenta un elemento dell'inventario nel sistema.
 */
export interface InventoryItem {
  productId: number;
  availableQuantity: number;
  reservedQuantity: number;
}

/**
 * Interfaccia per la richiesta di aggiustamento inventario
 */
export interface AdjustInventoryRequest {
  productId: number;
  delta: number;  // Può essere positivo (aggiunge) o negativo (rimuove)
}

/**
 * Interfaccia per la richiesta di impostazione inventario
 */
export interface SetInventoryRequest {
  productId: number;
  quantity: number;
}
