namespace CustomerService.Controllers;

using CustomerService.Models;
using CustomerService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;
    private readonly ILogger<WishlistController> _logger;

    public WishlistController(IWishlistService wishlistService, ILogger<WishlistController> logger)
    {
        _wishlistService = wishlistService;
        _logger = logger;
    }

    /// <summary>
    /// Get user's wishlist
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(Wishlist), StatusCodes.Status200OK)]
    public async Task<ActionResult<Wishlist>> GetWishlist(Guid userId, CancellationToken ct)
    {
        var wishlist = await _wishlistService.GetWishlistAsync(userId, ct);
        return Ok(wishlist);
    }

    /// <summary>
    /// Add item to wishlist
    /// </summary>
    [HttpPost("{userId:guid}/items")]
    [ProducesResponseType(typeof(WishlistItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WishlistItem>> AddToWishlist(
        Guid userId,
        [FromBody] AddToWishlistRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var item = await _wishlistService.AddToWishlistAsync(userId, request.ProductId, ct);
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
        var success = await _wishlistService.RemoveFromWishlistAsync(userId, productId, ct);
        
        if (!success)
        {
            return NotFound($"Product {productId} not found in wishlist");
        }

        return NoContent();
    }

    /// <summary>
    /// Move item from wishlist to cart
    /// </summary>
    [HttpPost("{userId:guid}/items/{productId:guid}/move-to-cart")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> MoveToCart(
        Guid userId,
        Guid productId,
        [FromBody] MoveToCartRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _wishlistService.MoveToCartAsync(userId, productId, request.SessionId, ct);
        
        if (!success)
        {
            return NotFound($"Product {productId} not found in wishlist");
        }

        return Ok(new { message = "Item moved to cart successfully" });
    }
}

public record AddToWishlistRequest([Required] Guid ProductId);

public record MoveToCartRequest([Required] string SessionId);
