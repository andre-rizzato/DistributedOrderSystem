namespace CustomerService.Controllers;

using CustomerService.Models;
using CustomerService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService, ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// Get shopping cart
    /// </summary>
    [HttpGet("{sessionId}")]
    [ProducesResponseType(typeof(Cart), StatusCodes.Status200OK)]
    public async Task<ActionResult<Cart>> GetCart(string sessionId, CancellationToken ct)
    {
        var cart = await _cartService.GetCartAsync(sessionId, ct);
        return Ok(cart);
    }

    /// <summary>
    /// Add item to cart
    /// </summary>
    [HttpPost("{sessionId}/items")]
    [ProducesResponseType(typeof(CartItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartItem>> AddToCart(
        string sessionId,
        [FromBody] AddToCartRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var item = await _cartService.AddToCartAsync(
            sessionId,
            request.ProductId,
            request.Quantity,
            request.UnitPrice,
            ct);

        return CreatedAtAction(nameof(GetCart), new { sessionId }, item);
    }

    /// <summary>
    /// Update cart item quantity
    /// </summary>
    [HttpPut("{sessionId}/items/{cartItemId}")]
    [ProducesResponseType(typeof(CartItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartItem>> UpdateCartItem(
        string sessionId,
        Guid cartItemId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var item = await _cartService.UpdateCartItemAsync(sessionId, cartItemId, request.Quantity, ct);
        
        if (item == null)
        {
            return NotFound($"Cart item {cartItemId} not found");
        }

        return Ok(item);
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    [HttpDelete("{sessionId}/items/{cartItemId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveFromCart(
        string sessionId,
        Guid cartItemId,
        CancellationToken ct)
    {
        var success = await _cartService.RemoveFromCartAsync(sessionId, cartItemId, ct);
        
        if (!success)
        {
            return NotFound($"Cart item {cartItemId} not found");
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
        await _cartService.ClearCartAsync(sessionId, ct);
        return NoContent();
    }
}

public record AddToCartRequest(
    [Required] Guid ProductId,
    [Required] [Range(1, int.MaxValue)] int Quantity,
    [Required] [Range(0.01, double.MaxValue)] decimal UnitPrice
);

public record UpdateCartItemRequest(
    [Required] [Range(1, int.MaxValue)] int Quantity
);
