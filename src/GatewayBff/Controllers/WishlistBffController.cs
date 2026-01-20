namespace GatewayBff.Controllers;

using GatewayBff.Commands;
using GatewayBff.Contracts;
using GatewayBff.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/wishlist")]
[Produces("application/json")]
public class WishlistBffController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WishlistBffController> _logger;

    public WishlistBffController(IMediator mediator, ILogger<WishlistBffController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get user's wishlist
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(WishlistDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WishlistDto>> GetWishlist(Guid userId, CancellationToken ct)
    {
        var wishlist = await _mediator.Send(new GetWishlistQuery(userId), ct);
        
        if (wishlist == null)
        {
            return NotFound();
        }

        return Ok(wishlist);
    }

    /// <summary>
    /// Add item to wishlist
    /// </summary>
    [HttpPost("{userId:guid}/items")]
    [ProducesResponseType(typeof(WishlistItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WishlistItemDto>> AddToWishlist(
        Guid userId,
        [FromBody] AddToWishlistRequest request,
        CancellationToken ct)
    {
        var item = await _mediator.Send(
            new AddToWishlistCommand(userId, request.ProductId),
            ct);

        if (item == null)
        {
            return BadRequest("Failed to add item to wishlist");
        }

        return CreatedAtAction(nameof(GetWishlist), new { userId }, item);
    }

    /// <summary>
    /// Remove item from wishlist
    /// </summary>
    [HttpDelete("{userId:guid}/items/{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveFromWishlist(
        Guid userId,
        Guid productId,
        CancellationToken ct)
    {
        var success = await _mediator.Send(new RemoveFromWishlistCommand(userId, productId), ct);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Move item from wishlist to cart
    /// </summary>
    [HttpPost("{userId:guid}/items/{productId:guid}/move-to-cart")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MoveToCart(
        Guid userId,
        Guid productId,
        [FromBody] MoveToCartRequest request,
        CancellationToken ct)
    {
        var success = await _mediator.Send(
            new MoveWishlistToCartCommand(userId, productId, request.SessionId),
            ct);

        if (!success)
        {
            return NotFound();
        }

        return Ok(new { message = "Item moved to cart successfully" });
    }
}
