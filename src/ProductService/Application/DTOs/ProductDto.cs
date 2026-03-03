namespace ProductService.Application.DTOs;

/// <summary>
/// DTO di lettura per le query sui prodotti.
/// Separato dal modello di scrittura (Product entity) — questo è il cuore di CQRS.
/// Le Query restituiscono DTO read-only, i Command operano sulle entità di dominio.
/// </summary>
public record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    string Description,
    bool IsActive);
