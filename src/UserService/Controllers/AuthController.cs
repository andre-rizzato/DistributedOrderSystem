using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Models.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ICommunicationService _communicationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ICommunicationService communicationService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _communicationService = communicationService;
        _logger = logger;
    }

    /// <summary>
    /// Registrazione nuovo utente
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Email già registrata" });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        // Assegna ruolo default
        await _userManager.AddToRoleAsync(user, "Customer");

        // Invia email di verifica
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var verificationUrl = $"{Request.Scheme}://{Request.Host}/api/auth/verify-email?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";
        await _communicationService.SendEmailVerificationAsync(user.Email, verificationUrl);
        await _communicationService.SendWelcomeEmailAsync(user.Email, user.FirstName!);

        // Genera tokens
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, GetIpAddress());
        await _tokenService.SaveRefreshTokenAsync(refreshToken);

        _logger.LogInformation("Nuovo utente registrato: {Email}", user.Email);

        var response = new AuthResponse(
            user.Id,
            user.Email,
            user.FirstName!,
            user.LastName!,
            accessToken,
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(15)
        );

        return CreatedAtAction(nameof(GetProfile), new { userId = user.Id }, response);
    }

    /// <summary>
    /// Login con email e password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            return Unauthorized(new { message = "Email o password non validi" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return Unauthorized(new { message = "Account bloccato temporaneamente" });
            
            return Unauthorized(new { message = "Email o password non validi" });
        }

        // Aggiorna ultimo login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Genera tokens
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, GetIpAddress());
        await _tokenService.SaveRefreshTokenAsync(refreshToken);

        _logger.LogInformation("Utente autenticato: {Email}", user.Email);

        return Ok(new AuthResponse(
            user.Id,
            user.Email!,
            user.FirstName ?? "",
            user.LastName ?? "",
            accessToken,
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(15)
        ));
    }

    /// <summary>
    /// Login con Google o Facebook
    /// </summary>
    [HttpPost("social-login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> SocialLogin([FromBody] SocialLoginRequest request)
    {
        // TODO: Validare il token con Google/Facebook API
        // Per ora, accetta qualsiasi token (implementare validazione in produzione!)

        ApplicationUser? user = null;

        if (request.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
        {
            user = await _userManager.Users.FirstOrDefaultAsync(u => u.GoogleId == request.Token);
        }
        else if (request.Provider.Equals("Facebook", StringComparison.OrdinalIgnoreCase))
        {
            user = await _userManager.Users.FirstOrDefaultAsync(u => u.FacebookId == request.Token);
        }

        // Se l'utente non esiste, crealo
        if (user == null && !string.IsNullOrEmpty(request.Email))
        {
            user = await _userManager.FindByEmailAsync(request.Email);
            
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    EmailConfirmed = true // Social login implica email verificata
                };

                if (request.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
                    user.GoogleId = request.Token;
                else if (request.Provider.Equals("Facebook", StringComparison.OrdinalIgnoreCase))
                    user.FacebookId = request.Token;

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
                }

                await _userManager.AddToRoleAsync(user, "Customer");
                await _communicationService.SendWelcomeEmailAsync(user.Email, user.FirstName!);
            }
            else
            {
                // Associa social ID all'utente esistente
                if (request.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
                    user.GoogleId = request.Token;
                else if (request.Provider.Equals("Facebook", StringComparison.OrdinalIgnoreCase))
                    user.FacebookId = request.Token;

                await _userManager.UpdateAsync(user);
            }
        }

        if (user == null)
        {
            return BadRequest(new { message = "Impossibile autenticare con social login" });
        }

        // Aggiorna ultimo login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Genera tokens
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, GetIpAddress());
        await _tokenService.SaveRefreshTokenAsync(refreshToken);

        _logger.LogInformation("Utente autenticato con {Provider}: {Email}", request.Provider, user.Email);

        return Ok(new AuthResponse(
            user.Id,
            user.Email!,
            user.FirstName ?? "",
            user.LastName ?? "",
            accessToken,
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(15)
        ));
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var refreshToken = await _tokenService.GetRefreshTokenAsync(request.RefreshToken);
        
        if (refreshToken == null || !refreshToken.IsActive)
        {
            return Unauthorized(new { message = "Token non valido" });
        }

        var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());
        if (user == null || !user.IsActive)
        {
            return Unauthorized(new { message = "Utente non trovato" });
        }

        // Revoca il vecchio token
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, GetIpAddress());

        // Genera nuovi tokens
        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id, GetIpAddress());
        await _tokenService.SaveRefreshTokenAsync(newRefreshToken);

        return Ok(new AuthResponse(
            user.Id,
            user.Email!,
            user.FirstName ?? "",
            user.LastName ?? "",
            newAccessToken,
            newRefreshToken.Token,
            DateTime.UtcNow.AddMinutes(15)
        ));
    }

    /// <summary>
    /// Logout (revoca refresh token)
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, GetIpAddress());
        _logger.LogInformation("Utente disconnesso");
        return NoContent();
    }

    /// <summary>
    /// Verifica email
    /// </summary>
    [HttpGet("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> VerifyEmail([FromQuery] string token, [FromQuery] string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return BadRequest(new { message = "Utente non trovato" });
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Token non valido" });
        }

        _logger.LogInformation("Email verificata per utente: {Email}", email);
        return Ok(new { message = "Email verificata con successo" });
    }

    /// <summary>
    /// Richiedi reset password
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        
        // Non rivelare se l'email existe o no (sicurezza)
        if (user == null)
        {
            return Ok(new { message = "Se l'email esiste, riceverai un link per resettare la password" });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = $"{Request.Scheme}://{Request.Host}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";
        
        await _communicationService.SendPasswordResetAsync(user.Email!, resetUrl);

        _logger.LogInformation("Richiesta reset password per: {Email}", request.Email);
        return Ok(new { message = "Se l'email esiste, riceverai un link per resettare la password" });
    }

    /// <summary>
    /// Reset password con token
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new { message = "Utente non trovato" });
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await _communicationService.SendPasswordChangedNotificationAsync(user.Email!);

        _logger.LogInformation("Password resettata per utente: {Email}", request.Email);
        return Ok(new { message = "Password resettata con successo" });
    }

    /// <summary>
    /// Cambia password (autenticato)
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        
        if (user == null)
        {
            return Unauthorized();
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        // Revoca tutti i refresh token esistenti
        await _tokenService.RevokeAllUserTokensAsync(user.Id);

        await _communicationService.SendPasswordChangedNotificationAsync(user.Email!);

        _logger.LogInformation("Password cambiata per utente: {Email}", user.Email);
        return Ok(new { message = "Password cambiata con successo" });
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("profile/{userId}")]
    public ActionResult GetProfile(Guid userId)
    {
        // Endpoint dummy per CreatedAtAction
        return Ok();
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
