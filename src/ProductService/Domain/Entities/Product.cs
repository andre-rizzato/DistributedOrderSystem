namespace ProductService.Domain.Entities;

/// <summary>
/// Entità di dominio Product.
/// Il Domain layer non ha dipendenze esterne (nessun riferimento a EF Core, Redis, ecc.).
/// Questo è il principio fondamentale della Clean Architecture: il dominio è al centro.
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
