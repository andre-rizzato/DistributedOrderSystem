namespace OrderService.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Aggregates;
using OrderService.Domain.ValueObjects;

/// <summary>
/// DbContext EF Core per il bounded context degli ordini.
/// Configura il mapping tra gli oggetti di dominio DDD e il database relazionale.
/// I Value Object (Money, OrderStatus) sono mappati tramite HasConversion.
/// </summary>
public class OrderContext : DbContext
{
    public OrderContext(DbContextOptions<OrderContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Value Object: OrderStatus → string
            entity.Property(e => e.Status)
                .HasConversion(
                    v => v.Value,
                    v => OrderStatus.From(v))
                .IsRequired()
                .HasMaxLength(50);

            // Value Object: Money → decimal
            entity.Property(e => e.Total)
                .HasConversion(
                    v => v.Amount,
                    v => new Money(v))
                .HasPrecision(18, 2);

            entity.Property(e => e.CreatedAt).IsRequired();

            // Ignora la proprietà DomainEvents (non va persistita)
            entity.Ignore(e => e.DomainEvents);

            // Configura la navigazione Items per usare il backing field _items
            entity.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            var nav = entity.Metadata.FindNavigation(nameof(Order.Items))!;
            nav.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Value Object: Money → decimal
            entity.Property(e => e.UnitPrice)
                .HasConversion(
                    v => v.Amount,
                    v => new Money(v))
                .HasPrecision(18, 2);

            entity.Property(e => e.Quantity);
            entity.Property(e => e.ProductId);
        });
    }
}
