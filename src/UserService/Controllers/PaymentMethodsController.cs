using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;
using UserService.Models.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[Authorize]
[ApiController]
[Route("api/users/{userId:guid}/payment-methods")]
[Produces("application/json")]
public class PaymentMethodsController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly IPaymentEncryptionService _encryptionService;
    private readonly ILogger<PaymentMethodsController> _logger;

    public PaymentMethodsController(
        UserDbContext context,
        IPaymentEncryptionService encryptionService,
        ILogger<PaymentMethodsController> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all of the user's payment methods
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PaymentMethodDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PaymentMethodDto>>> GetPaymentMethods(Guid userId)
    {
        if (!await CanAccessUser(userId))
        {
            return Forbid();
        }

        var methods = await _context.PaymentMethods
            .Where(pm => pm.UserId == userId)
            .OrderByDescending(pm => pm.IsDefault)
            .ThenByDescending(pm => pm.CreatedAt)
            .ToListAsync();

        return Ok(methods.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Get payment method by ID
    /// </summary>
    [HttpGet("{paymentMethodId:guid}")]
    [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentMethodDto>> GetPaymentMethod(Guid userId, Guid paymentMethodId)
    {
        if (!await CanAccessUser(userId))
        {
            return Forbid();
        }

        var method = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Id == paymentMethodId && pm.UserId == userId);

        if (method == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(method));
    }

    /// <summary>
    /// Add a new payment method
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentMethodDto>> CreatePaymentMethod(
        Guid userId,
        [FromBody] CreatePaymentMethodRequest request)
    {
        if (!await CanAccessUser(userId))
        {
            return Forbid();
        }

        // Credit card validation (basic)
        var cleanCardNumber = request.CardNumber.Replace(" ", "").Replace("-", "");
        if (cleanCardNumber.Length < 13 || cleanCardNumber.Length > 19)
        {
            return BadRequest(new { message = "Invalid card number" });
        }

        // If this is the default, clear the default flag on the others
        if (request.IsDefault)
        {
            await RemoveDefaultFlag(userId);
        }

        var paymentMethod = new PaymentMethod
        {
            UserId = userId,
            Type = request.Type,
            CardHolderName = request.CardHolderName,
            Last4Digits = _encryptionService.GetLast4Digits(cleanCardNumber),
            CardBrand = request.CardBrand,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            EncryptedToken = _encryptionService.Encrypt(cleanCardNumber),
            IsDefault = request.IsDefault
        };

        _context.PaymentMethods.Add(paymentMethod);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment method created for user {UserId}", userId);
        return CreatedAtAction(
            nameof(GetPaymentMethod),
            new { userId, paymentMethodId = paymentMethod.Id },
            MapToDto(paymentMethod));
    }

    /// <summary>
    /// Delete payment method
    /// </summary>
    [HttpDelete("{paymentMethodId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeletePaymentMethod(Guid userId, Guid paymentMethodId)
    {
        if (!await CanAccessUser(userId))
        {
            return Forbid();
        }

        var method = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Id == paymentMethodId && pm.UserId == userId);

        if (method == null)
        {
            return NotFound();
        }

        _context.PaymentMethods.Remove(method);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment method {PaymentMethodId} deleted for user {UserId}",
            paymentMethodId, userId);
        return NoContent();
    }

    /// <summary>
    /// Set payment method as default
    /// </summary>
    [HttpPost("{paymentMethodId:guid}/set-default")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetAsDefault(Guid userId, Guid paymentMethodId)
    {
        if (!await CanAccessUser(userId))
        {
            return Forbid();
        }

        var method = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Id == paymentMethodId && pm.UserId == userId);

        if (method == null)
        {
            return NotFound();
        }

        await RemoveDefaultFlag(userId);
        method.IsDefault = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Payment method set as default" });
    }

    private async Task<bool> CanAccessUser(Guid userId)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return currentUserId == userId.ToString();
    }

    private async Task RemoveDefaultFlag(Guid userId)
    {
        var defaultMethods = await _context.PaymentMethods
            .Where(pm => pm.UserId == userId && pm.IsDefault)
            .ToListAsync();

        foreach (var method in defaultMethods)
        {
            method.IsDefault = false;
        }
    }

    private PaymentMethodDto MapToDto(PaymentMethod method)
    {
        return new PaymentMethodDto(
            method.Id,
            method.Type.ToString(),
            method.CardHolderName,
            method.Last4Digits,
            method.CardBrand,
            method.ExpiryMonth,
            method.ExpiryYear,
            method.IsDefault,
            method.IsExpired
        );
    }
}
