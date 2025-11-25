namespace InventoryService.Models;
public class InventoryItem
{
    public int Id { get; set; } //Primary Key
    public int ProductId { get; set; } = 0;//Foreign Key to Product
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}