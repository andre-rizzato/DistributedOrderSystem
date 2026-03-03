using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using ProductService.Application.Commands.CreateProduct;
using ProductService.Application.Commands.UpdateProduct;
using ProductService.Application.Commands.DeleteProduct;
using ProductService.Application.Queries.GetAllProducts;
using ProductService.Application.Queries.GetProductById;
using ProductService.Application.Queries.SearchProducts;
using ProductService.Application.DTOs;

namespace ProductService.Controllers;

/// <summary>
/// Controller thin per i prodotti — Clean Architecture + Full CQRS.
/// OGNI operazione passa attraverso MediatR (Send) come Command o Query.
/// Il controller non contiene logica di business: solo mapping HTTP → CQRS.
/// 
/// Pipeline MediatR:
///   Request → ValidationBehavior → LoggingBehavior → Handler → Response
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // ── QUERIES (read-side) ──────────────────────────────────────

    /// <summary>GET api/products</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllProductsQuery(), ct);
        return Ok(result);
    }

    /// <summary>GET api/products/{id}</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        if (result is null) return NotFound($"Product with ID {id} not found");
        return Ok(result);
    }

    /// <summary>GET api/products/search</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> SearchProducts(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool? isActive = true,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new SearchProductsQuery(searchTerm, category, minPrice, maxPrice, isActive), ct);
        return Ok(result);
    }

    /// <summary>GET api/products/featured</summary>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetFeaturedProducts(
        [FromQuery] int count = 10, CancellationToken ct = default)
    {
        var all = await _mediator.Send(new GetAllProductsQuery(), ct);
        var featured = all.Where(p => p.IsActive).OrderByDescending(p => p.Price).Take(count);
        return Ok(featured);
    }

    /// <summary>GET api/products/bestsellers</summary>
    [HttpGet("bestsellers")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetBestsellers(
        [FromQuery] int count = 10, CancellationToken ct = default)
    {
        var all = await _mediator.Send(new GetAllProductsQuery(), ct);
        var bestsellers = all.Where(p => p.IsActive).Take(count);
        return Ok(bestsellers);
    }

    /// <summary>GET api/products/recommended</summary>
    [HttpGet("recommended")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetRecommendedProducts(
        [FromQuery] int count = 10, CancellationToken ct = default)
    {
        var all = await _mediator.Send(new GetAllProductsQuery(), ct);
        var recommended = all.Where(p => p.IsActive).Take(count);
        return Ok(recommended);
    }

    /// <summary>GET api/products/{productId}/related</summary>
    [HttpGet("{productId:guid}/related")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetRelatedProducts(
        Guid productId, [FromQuery] int count = 5, CancellationToken ct = default)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(productId), ct);
        if (product is null) return NotFound($"Product with ID {productId} not found");

        var all = await _mediator.Send(new GetAllProductsQuery(), ct);
        var related = all.Where(p => p.IsActive && p.Id != productId).Take(count);
        return Ok(related);
    }

    /// <summary>GET api/categories</summary>
    [HttpGet("~/api/categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetCategories()
    {
        var categories = new List<string>
        {
            "Electronics", "Clothing", "Home & Garden", "Sports", "Books", "Toys", "Food & Beverages"
        };
        return Ok(categories);
    }

    /// <summary>GET api/categories/{categoryName}/products</summary>
    [HttpGet("~/api/categories/{categoryName}/products")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByCategory(
        string categoryName, CancellationToken ct = default)
    {
        var all = await _mediator.Send(new GetAllProductsQuery(), ct);
        return Ok(all.Where(p => p.IsActive));
    }

    /// <summary>GET api/products/brands</summary>
    [HttpGet("brands")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetBrands()
    {
        return Ok(new List<string> { "BrandA", "BrandB", "BrandC", "BrandD", "BrandE" });
    }

    /// <summary>GET api/products/{productId}/reviews</summary>
    [HttpGet("{productId:guid}/reviews")]
    [ProducesResponseType(typeof(IEnumerable<ProductReview>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProductReview>>> GetProductReviews(
        Guid productId, CancellationToken ct = default)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(productId), ct);
        if (product is null) return NotFound($"Product with ID {productId} not found");
        return Ok(new List<ProductReview>());
    }

    /// <summary>POST api/products/{productId}/reviews</summary>
    [HttpPost("{productId:guid}/reviews")]
    [ProducesResponseType(typeof(ProductReview), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductReview>> AddProductReview(
        Guid productId, [FromBody] CreateReviewRequest review, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var product = await _mediator.Send(new GetProductByIdQuery(productId), ct);
        if (product is null) return NotFound($"Product with ID {productId} not found");

        var newReview = new ProductReview
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Rating = review.Rating,
            Comment = review.Comment,
            ReviewerName = review.ReviewerName,
            CreatedAt = DateTime.UtcNow
        };

        return CreatedAtAction(nameof(GetProductReviews), new { productId }, newReview);
    }

    // ── COMMANDS (write-side) ────────────────────────────────────

    /// <summary>POST api/products</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        [FromBody] CreateProductRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _mediator.Send(
                new CreateProductCommand(request.Name, request.Price, request.Description), ct);
            return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(ex.Errors.Select(e => e.ErrorMessage));
        }
    }

    /// <summary>PUT api/products/{id}</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(
        Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _mediator.Send(
                new UpdateProductCommand(id, request.Name, request.Price, request.Description, request.IsActive), ct);
            if (result is null) return NotFound($"Product with ID {id} not found");
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(ex.Errors.Select(e => e.ErrorMessage));
        }
    }

    /// <summary>DELETE api/products/{id}</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteProduct(Guid id, CancellationToken ct)
    {
        var deleted = await _mediator.Send(new DeleteProductCommand(id), ct);
        if (!deleted) return NotFound($"Product with ID {id} not found");
        return NoContent();
    }
}

// ── Request/Response DTOs (Presentation layer) ──────────────────

public record CreateProductRequest(
    [Required] string Name,
    [Required] decimal Price,
    string? Description);

public record UpdateProductRequest(
    [Required] string Name,
    [Required] decimal Price,
    string? Description,
    bool IsActive);

public class ProductReview
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewRequest
{
    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Comment { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ReviewerName { get; set; } = string.Empty;
}
