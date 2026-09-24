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
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Email already registered" });
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

        // Assign default role
        await _userManager.AddToRoleAsync(user, "Customer");

        // Send verification email
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var verificationUrl = $"{Request.Scheme}://{Request.Host}/api/auth/verify-email?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";
        await _communicationService.SendEmailVerificationAsync(user.Email, verificationUrl);
        await _communicationService.SendWelcomeEmailAsync(user.Email, user.FirstName!);

        // Generate tokens
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, GetIpAddress());
        await _tokenService.SaveRefreshTokenAsync(refreshToken);

        _logger.LogInformation("New user registered: {Email}", user.Email);

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
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return Unauthorized(new { message = "Account temporarily locked" });

            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, GetIpAddress());
        await _tokenService.SaveRefreshTokenAsync(refreshToken);

        _logger.LogInformation("User authenticated: {Email}", user.Email);

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
    /// Login with Google or Facebook
    /// </summary>
    [HttpPost("social-login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> SocialLogin([FromBody] SocialLoginRequest request)
    {
        // TODO: Validate the token with the Google/Facebook API
        // For now, accept any token (implement real validation in production!)

        ApplicationUser? user = null;

        if (request.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
        {
            user = await _userManager.Users.FirstOrDefaultAsync(u => u.GoogleId == request.Token);
        }
        else if (request.Provider.Equals("Facebook", StringComparison.OrdinalIgnoreCase))
        {
            user = await _userManager.Users.FirstOrDefaultAsync(u => u.FacebookId == request.Token);
        }

        // If the user doesn't exist yet, create it
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
                    EmailConfirmed = true // Social login implies a verified email
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
                // Link the social ID to the existing user
                if (request.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
                    user.GoogleId = request.Token;
                else if (request.Provider.Equals("Facebook", StringComparison.OrdinalIgnoreCase))
                    user.FacebookId = request.Token;

                await _userManager.UpdateAsync(user);
            }
        }

        if (user == null)
        {
            return BadRequest(new { message = "Unable to authenticate with social login" });
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, GetIpAddress());
        await _tokenService.SaveRefreshTokenAsync(refreshToken);

        _logger.LogInformation("User authenticated with {Provider}: {Email}", request.Provider, user.Email);

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
            return Unauthorized(new { message = "Invalid token" });
        }

        var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());
        if (user == null || !user.IsActive)
        {
            return Unauthorized(new { message = "User not found" });
        }

        // Revoke the old token
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, GetIpAddress());

        // Generate new tokens
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
    /// Logout (revokes refresh token)
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, GetIpAddress());
        _logger.LogInformation("User logged out");
        return NoContent();
    }

    /// <summary>
    /// Verify email
    /// </summary>
    [HttpGet("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> VerifyEmail([FromQuery] string token, [FromQuery] string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return BadRequest(new { message = "User not found" });
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Invalid token" });
        }

        _logger.LogInformation("Email verified for user: {Email}", email);
        return Ok(new { message = "Email verified successfully" });
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Don't reveal whether the email exists or not (security)
        if (user == null)
        {
            return Ok(new { message = "If the email exists, you'll receive a link to reset your password" });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = $"{Request.Scheme}://{Request.Host}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";

        await _communicationService.SendPasswordResetAsync(user.Email!, resetUrl);

        _logger.LogInformation("Password reset requested for: {Email}", request.Email);
        return Ok(new { message = "If the email exists, you'll receive a link to reset your password" });
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new { message = "User not found" });
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await _communicationService.SendPasswordChangedNotificationAsync(user.Email!);

        _logger.LogInformation("Password reset for user: {Email}", request.Email);
        return Ok(new { message = "Password reset successfully" });
    }

    /// <summary>
    /// Change password (authenticated)
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

        // Revoke all existing refresh tokens
        await _tokenService.RevokeAllUserTokensAsync(user.Id);

        await _communicationService.SendPasswordChangedNotificationAsync(user.Email!);

        _logger.LogInformation("Password changed for user: {Email}", user.Email);
        return Ok(new { message = "Password changed successfully" });
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("profile/{userId}")]
    public ActionResult GetProfile(Guid userId)
    {
        // Dummy endpoint for CreatedAtAction
        return Ok();
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
