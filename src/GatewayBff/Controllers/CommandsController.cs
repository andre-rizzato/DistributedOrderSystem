
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

    [HttpPost("products")]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var command = new CreateProductCommand(request.Name, request.Price, request.Description);
        var result = await _mediator.Send(command, ct);
        
        return CreatedAtAction(
            actionName: "GetCatalogItem",
            controllerName: "Queries",
            routeValues: new { id = result.Id },
            value: result
        );
    }

    [HttpPut("products/{id:int}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(
        int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var command = new UpdateProductCommand(id, request.Name, request.Price, request.Description, request.IsActive);
        var result = await _mediator.Send(command, ct);
        
        return Ok(result);
    }

    [HttpDelete("products/{id:int}")]
    public async Task<ActionResult> DeleteProduct(int id, CancellationToken ct)
    {
        var command = new DeleteProductCommand(id);
        var success = await _mediator.Send(command, ct);
        
        if (!success)
            return NotFound($"Product with ID {id} not found");

        return NoContent();
    }
}

public record CreateProductRequest(string Name, decimal Price, string? Description);
public record UpdateProductRequest(string Name, decimal Price, string? Description, bool IsActive);
