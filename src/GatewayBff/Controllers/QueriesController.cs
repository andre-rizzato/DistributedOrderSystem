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
}
