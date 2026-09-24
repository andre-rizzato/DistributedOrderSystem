/**
 * InventoryItem interface
 *
 * Represents an inventory item in the system.
 */
export interface InventoryItem {
  productId: number;
  availableQuantity: number;
  reservedQuantity: number;
}

/**
 * Interface for the inventory adjustment request
 */
export interface AdjustInventoryRequest {
  productId: number;
  delta: number;  // Can be positive (adds) or negative (removes)
}

/**
 * Interface for the inventory set request
 */
export interface SetInventoryRequest {
  productId: number;
  quantity: number;
}
