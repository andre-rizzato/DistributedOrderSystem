namespace ProductService.Domain.Entities;

/// <summary>
/// Product domain entity.
/// The Domain layer has no external dependencies (no reference to EF Core, Redis, etc.).
/// This is the core principle of Clean Architecture: the domain is at the center.
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
