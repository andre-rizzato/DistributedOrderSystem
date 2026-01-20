namespace GatewayBff.Controllers;

using GatewayBff.Commands;
using GatewayBff.Contracts;
using GatewayBff.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/cart")]
[Produces("application/json")]
public class CartBffController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CartBffController> _logger;

    public CartBffController(IMediator mediator, ILogger<CartBffController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get shopping cart
    /// </summary>
    [HttpGet("{sessionId}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> GetCart(string sessionId, CancellationToken ct)
    {
        var cart = await _mediator.Send(new GetCartQuery(sessionId), ct);
        
        if (cart == null)
        {
            return NotFound();
        }

        return Ok(cart);
    }

    /// <summary>
    /// Add item to cart
    /// </summary>
    [HttpPost("{sessionId}/items")]
    [ProducesResponseType(typeof(CartItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartItemDto>> AddToCart(
        string sessionId,
        [FromBody] AddToCartRequest request,
        CancellationToken ct)
    {
        var item = await _mediator.Send(
            new AddToCartCommand(sessionId, request.ProductId, request.Quantity, request.UnitPrice),
            ct);

        if (item == null)
        {
            return BadRequest("Failed to add item to cart");
        }

        return CreatedAtAction(nameof(GetCart), new { sessionId }, item);
    }

    /// <summary>
    /// Update cart item quantity
    /// </summary>
    [HttpPut("{sessionId}/items/{cartItemId:guid}")]
    [ProducesResponseType(typeof(CartItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartItemDto>> UpdateCartItem(
        string sessionId,
        Guid cartItemId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken ct)
    {
        var item = await _mediator.Send(
            new UpdateCartItemCommand(sessionId, cartItemId, request.Quantity),
            ct);

        if (item == null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    [HttpDelete("{sessionId}/items/{cartItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveFromCart(
        string sessionId,
        Guid cartItemId,
        CancellationToken ct)
    {
        var success = await _mediator.Send(new RemoveFromCartCommand(sessionId, cartItemId), ct);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Clear entire cart
    /// </summary>
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> ClearCart(string sessionId, CancellationToken ct)
    {
        await _mediator.Send(new ClearCartCommand(sessionId), ct);
        return NoContent();
    }
}
