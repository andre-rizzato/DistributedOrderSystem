using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using UserService.Models.DTOs;

namespace UserService.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserManager<ApplicationUser> userManager, ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Ottieni profilo utente corrente
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(MapToDto(user));
    }

    /// <summary>
    /// Ottieni profilo utente per ID
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetUserById(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        
        if (user == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(user));
    }

    /// <summary>
    /// Aggiorna profilo utente
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        
        if (user == null)
        {
            return Unauthorized();
        }

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
        if (request.DateOfBirth.HasValue) user.DateOfBirth = request.DateOfBirth;
        if (request.ProfilePictureUrl != null) user.ProfilePictureUrl = request.ProfilePictureUrl;

        var result = await _userManager.UpdateAsync(user);
        
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("Profilo aggiornato per utente: {Email}", user.Email);
        return Ok(MapToDto(user));
    }

    /// <summary>
    /// Aggiorna preferenze utente
    /// </summary>
    [HttpPut("me/preferences")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileDto>> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        
        if (user == null)
        {
            return Unauthorized();
        }

        if (request.PreferredLanguage != null) user.PreferredLanguage = request.PreferredLanguage;
        if (request.PreferredCurrency != null) user.PreferredCurrency = request.PreferredCurrency;
        if (request.EmailNotificationsEnabled.HasValue) user.EmailNotificationsEnabled = request.EmailNotificationsEnabled.Value;
        if (request.SmsNotificationsEnabled.HasValue) user.SmsNotificationsEnabled = request.SmsNotificationsEnabled.Value;
        if (request.PushNotificationsEnabled.HasValue) user.PushNotificationsEnabled = request.PushNotificationsEnabled.Value;

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Preferenze aggiornate per utente: {Email}", user.Email);
        return Ok(MapToDto(user));
    }

    /// <summary>
    /// Elimina account utente
    /// </summary>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteAccount()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        
        if (user == null)
        {
            return Unauthorized();
        }

        // Soft delete
        user.IsActive = false;
        await _userManager.UpdateAsync(user);

        _logger.LogWarning("Account disattivato per utente: {Email}", user.Email);
        return NoContent();
    }

    private UserProfileDto MapToDto(ApplicationUser user)
    {
        return new UserProfileDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.DateOfBirth,
            user.ProfilePictureUrl,
            user.PreferredLanguage,
            user.PreferredCurrency,
            user.EmailNotificationsEnabled,
            user.SmsNotificationsEnabled,
            user.PushNotificationsEnabled,
            user.IsPrimeMember,
            user.PrimeMembershipExpiry,
            user.LoyaltyPoints,
            user.CreatedAt,
            user.LastLoginAt
        );
    }
}
