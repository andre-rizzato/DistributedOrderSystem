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

    [HttpGet("catalog/{id:int}")]
    public async Task<ActionResult<CatalogItemDto>> GetCatalogItem(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        if (result == null)
            return NotFound($"Product with ID {id} not found");
        
        return Ok(result);
    }
}
