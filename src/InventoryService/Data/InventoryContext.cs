namespace InventoryService.Data;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

public class InventoryContext : DbContext
{
    public InventoryContext(DbContextOptions<InventoryContext> options) : base(options)
    {
    }

    public DbSet<InventoryItem> InventoryItems  => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(entity =>
        {
        
        entity.ToTable("Inventory");
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.ProductId).IsUnique();
        entity.Property(e => e.AvailableQuantity).IsRequired();
        entity.Property(e => e.ReservedQuantity).IsRequired();
        entity.Property(e => e.LastUpdatedUtc).IsRequired();
    });
    }
}



 