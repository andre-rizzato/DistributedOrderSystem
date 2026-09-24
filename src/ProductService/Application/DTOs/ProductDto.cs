namespace ProductService.Application.DTOs;

/// <summary>
/// Read DTO for product queries.
/// Separate from the write model (Product entity) — this is the heart of CQRS.
/// Queries return read-only DTOs, Commands operate on domain entities.
/// </summary>
public record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    string Description,
    bool IsActive);
