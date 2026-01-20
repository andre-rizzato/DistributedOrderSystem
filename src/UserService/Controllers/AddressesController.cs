using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;
using UserService.Models.DTOs;

namespace UserService.Controllers;

[Authorize]
[ApiController]
[Route("api/users/{userId:guid}/addresses")]
[Produces("application/json")]
public class AddressesController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AddressesController> _logger;

    public AddressesController(
        UserDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AddressesController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Ottieni tutti gli indirizzi dell'utente
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AddressDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AddressDto>>> GetAddresses(Guid userId)
    {
        if (!await CanAccessUser(userId))
        {
            return Forbid();
        }

        var addresses = await _context.Addresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Ok(addresses.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Ottieni indirizzo per ID
    /// </summary>
    [HttpGet("{addressId:guid}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressDto>> GetAddress(Guid userId, Guid addressId)
    {
        if (!await CanAccessUser(userId))
        {
            return Forbid();
        }

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

        if (address == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(address));
    }

    /// <summary>
    /// Crea nuovo indirizzo
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddressDto>> CreateAddress(Guid userId, [FromBody] CreateAddressRequest request)
    {
        if (!await CanAccessUser(userId))
        {
            return Forbid();
        }

        // Se è default, rimuovi default dagli altri
        if (request.IsDefault)
        {
            await RemoveDefaultFlag(userId, request.Type);
        }

        var address = new Address
        {
            UserId = userId,
            FullName = request.FullName,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            StateProvince = request.StateProvince,
            PostalCode = request.PostalCode,
            CountryCode = request.CountryCode,
            PhoneNumber = request.PhoneNumber,
            Type = request.Type,
            IsDefault = request.IsDefault
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Indirizzo creato per utente {UserId}", userId);
        return CreatedAtAction(nameof(GetAddress), new { userId, addressId = address.Id }, MapToDto(address));
    }

    /// <summary>
    /// Aggiorna indirizzo
    /// </summary>
    [HttpPut("{addressId:guid}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressDto>> UpdateAddress(
        Guid userId,
        Guid addressId,
        [FromBody] UpdateAddressRequest request)
    {
        if (!await CanAccessUser(userId))
        {
            return Forbid();
        }

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

        if (address == null)
        {
            return NotFound();
        }

        if (request.FullName != null) address.FullName = request.FullName;
        if (request.AddressLine1 != null) address.AddressLine1 = request.AddressLine1;
        if (request.AddressLine2 != null) address.AddressLine2 = request.AddressLine2;
        if (request.City != null) address.City = request.City;
        if (request.StateProvince != null) address.StateProvince = request.StateProvince;
        if (request.PostalCode != null) address.PostalCode = request.PostalCode;
        if (request.CountryCode != null) address.CountryCode = request.CountryCode;
        if (request.PhoneNumber != null) address.PhoneNumber = request.PhoneNumber;
        if (request.Type.HasValue) address.Type = request.Type.Value;
        
        if (request.IsDefault.HasValue && request.IsDefault.Value)
        {
            await RemoveDefaultFlag(userId, address.Type);
            address.IsDefault = true;
        }

        address.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Indirizzo {AddressId} aggiornato per utente {UserId}", addressId, userId);
        return Ok(MapToDto(address));
    }

    /// <summary>
    /// Elimina indirizzo
    /// </summary>
    [HttpDelete("{addressId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAddress(Guid userId, Guid addressId)
    {
        if (!await CanAccessUser(userId))
        {
            return Forbid();
        }

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

        if (address == null)
        {
            return NotFound();
        }

        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Indirizzo {AddressId} eliminato per utente {UserId}", addressId, userId);
        return NoContent();
    }

    /// <summary>
    /// Imposta indirizzo come default
    /// </summary>
    [HttpPost("{addressId:guid}/set-default")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetAsDefault(Guid userId, Guid addressId)
    {
        if (!await CanAccessUser(userId))
        {
            return Forbid();
        }

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

        if (address == null)
        {
            return NotFound();
        }

        await RemoveDefaultFlag(userId, address.Type);
        address.IsDefault = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Indirizzo impostato come default" });
    }

    private async Task<bool> CanAccessUser(Guid userId)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return currentUserId == userId.ToString();
    }

    private async Task RemoveDefaultFlag(Guid userId, AddressType type)
    {
        var defaultAddresses = await _context.Addresses
            .Where(a => a.UserId == userId && a.Type == type && a.IsDefault)
            .ToListAsync();

        foreach (var addr in defaultAddresses)
        {
            addr.IsDefault = false;
        }
    }

    private AddressDto MapToDto(Address address)
    {
        return new AddressDto(
            address.Id,
            address.FullName,
            address.AddressLine1,
            address.AddressLine2,
            address.City,
            address.StateProvince,
            address.PostalCode,
            address.CountryCode,
            address.PhoneNumber,
            address.Type.ToString(),
            address.IsDefault
        );
    }
}
