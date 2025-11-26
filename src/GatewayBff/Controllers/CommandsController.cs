
namespace GatewayBff.Controllers;

using GatewayBff.Commands;
using GatewayBff.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/commands")]
public class CommandsController : ControllerBase
{
    private readonly IMediator _mediator;
    public CommandsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("orders")]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder(
        [FromBody] CreateOrderRequest? request,
        CancellationToken ct)
    {
        if (request?.Items is null || request.Items.Count == 0)
        {
            return BadRequest("At least one item is required.");
        }

        var result = await _mediator.Send(new CreateOrderCommand(request.Items), ct);
        return Ok(result);
    }
}
