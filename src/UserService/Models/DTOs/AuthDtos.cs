using System.ComponentModel.DataAnnotations;

namespace UserService.Models.DTOs;

public record RegisterRequest(
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password,
    [Required] string FirstName,
    [Required] string LastName,
    string? PhoneNumber
);

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password
);

public record SocialLoginRequest(
    [Required] string Provider, // "Google" or "Facebook"
    [Required] string Token,
    string? Email,
    string? FirstName,
    string? LastName
);

public record AuthResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);

public record RefreshTokenRequest([Required] string RefreshToken);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required][MinLength(8)] string NewPassword
);

public record ForgotPasswordRequest([Required][EmailAddress] string Email);

public record ResetPasswordRequest(
    [Required] string Token,
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string NewPassword
);

public record VerifyEmailRequest([Required] string Token, [Required][EmailAddress] string Email);
