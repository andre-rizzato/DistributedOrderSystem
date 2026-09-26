namespace GatewayBff.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using GatewayBff.Contracts;
using GatewayBff.Queries;

[ApiController]
[Route("api/queries")]
public class QueriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public QueriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("catalog")]
    public async Task<ActionResult<List<CatalogItemDto>>> GetCatalog(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCatalogQuery(), ct);
        return Ok(result);
    }

    [HttpGet("catalog/{id:guid}")]
    public async Task<ActionResult<CatalogItemDto>> GetCatalogItem(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        if (result == null)
            return NotFound($"Product with ID {id} not found");

        return Ok(result);
    }

    [HttpGet("catalog/search")]
    public async Task<ActionResult<List<CatalogItemDto>>> SearchCatalog(
        [FromQuery] string? searchTerm, [FromQuery] string? category,
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new SearchCatalogQuery(searchTerm, category, minPrice, maxPrice), ct);
        return Ok(result);
    }

    [HttpGet("catalog/featured")]
    public async Task<ActionResult<List<CatalogItemDto>>> GetFeaturedCatalog([FromQuery] int count = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFeaturedCatalogQuery(count), ct);
        return Ok(result);
    }

    [HttpGet("catalog/bestsellers")]
    public async Task<ActionResult<List<CatalogItemDto>>> GetBestsellersCatalog([FromQuery] int count = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBestsellersCatalogQuery(count), ct);
        return Ok(result);
    }

    [HttpGet("catalog/recommended")]
    public async Task<ActionResult<List<CatalogItemDto>>> GetRecommendedCatalog([FromQuery] int count = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetRecommendedCatalogQuery(count), ct);
        return Ok(result);
    }

    [HttpGet("catalog/{id:guid}/related")]
    public async Task<ActionResult<List<CatalogItemDto>>> GetRelatedCatalog(Guid id, [FromQuery] int count = 5, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetRelatedCatalogQuery(id, count), ct);
        if (result is null)
            return NotFound($"Product with ID {id} not found");

        return Ok(result);
    }

    [HttpGet("catalog/{id:guid}/reviews")]
    public async Task<ActionResult<List<ReviewDto>>> GetProductReviews(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductReviewsQuery(id), ct);
        if (result is null)
            return NotFound($"Product with ID {id} not found");

        return Ok(result);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("categories/{categoryName}/products")]
    public async Task<ActionResult<List<CatalogItemDto>>> GetCatalogByCategory(string categoryName, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCatalogByCategoryQuery(categoryName), ct);
        return Ok(result);
    }

    [HttpGet("brands")]
    public async Task<ActionResult<List<string>>> GetBrands(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBrandsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("orders")]
    public async Task<ActionResult<List<OrderDto>>> GetOrders(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllOrdersQuery(), ct);
        return Ok(result);
    }

    [HttpGet("orders/{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id), ct);
        if (result == null)
            return NotFound($"Order with ID {id} not found");
        
        return Ok(result);
    }
}
