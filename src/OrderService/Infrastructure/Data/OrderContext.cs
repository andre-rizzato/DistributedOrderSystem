namespace OrderService.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Aggregates;
using OrderService.Domain.ValueObjects;
using OrderService.Infrastructure.Outbox;

/// <summary>
/// EF Core DbContext for the orders bounded context.
/// Configures the mapping between DDD domain objects and the relational database.
/// Value Objects (Money, OrderStatus) are mapped via HasConversion.
/// </summary>
public class OrderContext : DbContext
{
    public OrderContext(DbContextOptions<OrderContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Value Object: OrderStatus -> string
            entity.Property(e => e.Status)
                .HasConversion(
                    v => v.Value,
                    v => OrderStatus.From(v))
                .IsRequired()
                .HasMaxLength(50);

            // Value Object: Money -> decimal
            entity.Property(e => e.Total)
                .HasConversion(
                    v => v.Amount,
                    v => new Money(v))
                .HasPrecision(18, 2);

            entity.Property(e => e.CreatedAt).IsRequired();

            // Ignore the DomainEvents property (must not be persisted)
            entity.Ignore(e => e.DomainEvents);

            // Configure the Items navigation to use the _items backing field
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

            // Value Object: Money -> decimal
            entity.Property(e => e.UnitPrice)
                .HasConversion(
                    v => v.Amount,
                    v => new Money(v))
                .HasPrecision(18, 2);

            entity.Property(e => e.Quantity);
            entity.Property(e => e.ProductId);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.OccurredOnUtc).IsRequired();
            entity.HasIndex(e => new { e.ProcessedOnUtc, e.OccurredOnUtc });
        });
    }
}
