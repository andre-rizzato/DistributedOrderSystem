namespace ChatbotService.Services;

using ChatbotService.Models;
using ChatbotService.Services.Interfaces;
using ChatbotService.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

public class AuthenticationService : IAuthenticationService
{
    private readonly AuthSettings _authSettings;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IOptions<AuthSettings> authSettings,
        ILogger<AuthenticationService> logger)
    {
        _authSettings = authSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResponse> AuthenticateAsync(AuthRequest request)
    {
        try
        {
            // Simple authentication for demo - in production, verify against database/external provider
            if (await ValidateCredentialsAsync(request.Email, request.Password))
            {
                var user = await GetUserByEmailAsync(request.Email);
                if (user != null)
                {
                    var token = GenerateToken(user);
                    var refreshToken = GenerateRefreshToken();

                    return new AuthResponse
                    {
                        Success = true,
                        Token = token,
                        RefreshToken = refreshToken,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(_authSettings.TokenExpirationMinutes),
                        User = user
                    };
                }
            }

            return new AuthResponse
            {
                Success = false,
                ErrorMessage = "Credenziali non valide"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error for user {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                ErrorMessage = "Errore durante l'autenticazione"
            };
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        // Implementation for refresh token
        // In production, validate refresh token against database
        await Task.CompletedTask;
        
        return new AuthResponse
        {
            Success = false,
            ErrorMessage = "Refresh token non implementato"
        };
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_authSettings.SecretKey);
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _authSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _authSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserInfo?> GetUserInfoAsync(string userId)
    {
        // Mock implementation - in production, fetch from database
        await Task.CompletedTask;
        
        return new UserInfo
        {
            Id = userId,
            Email = "user@example.com",
            Name = "Test User",
            Roles = new List<string> { "Customer" }
        };
    }

    public string GenerateToken(UserInfo user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_authSettings.SecretKey);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("jti", Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddMinutes(_authSettings.TokenExpirationMinutes),
            Issuer = _authSettings.Issuer,
            Audience = _authSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        // Add role claims
        foreach (var role in user.Roles)
        {
            tokenDescriptor.Subject.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task<bool> ValidateCredentialsAsync(string email, string password)
    {
        // Mock validation - in production, hash and compare passwords
        await Task.CompletedTask;
        
        // Demo credentials
        return email == "admin@example.com" && password == "password123" ||
               email == "user@example.com" && password == "password123";
    }

    private async Task<UserInfo?> GetUserByEmailAsync(string email)
    {
        // Mock user data - in production, fetch from database
        await Task.CompletedTask;
        
        return email switch
        {
            "admin@example.com" => new UserInfo
            {
                Id = "admin-123",
                Email = email,
                Name = "Admin User",
                Roles = new List<string> { "Admin", "Customer" }
            },
            "user@example.com" => new UserInfo
            {
                Id = "user-456",
                Email = email,
                Name = "Regular User",
                Roles = new List<string> { "Customer" }
            },
            _ => null
        };
    }

    public async Task<AuthResponse> AuthenticateAsync(LoginRequest request)
    {
        // Simple demo authentication
        var authRequest = new AuthRequest
        {
            Email = request.Username,
            Password = request.Password
        };
        return await AuthenticateAsync(authRequest);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Simple demo registration
        return await Task.FromResult(new AuthResponse
        {
            Success = true,
            Token = GenerateToken(new UserInfo 
            { 
                Id = Guid.NewGuid().ToString(),
                Email = request.Email,
                Name = request.Name,
                Roles = new List<string> { "Customer" }
            }),
            ExpiresAt = DateTime.UtcNow.AddMinutes(_authSettings.TokenExpirationMinutes),
            User = new UserInfo
            {
                Id = Guid.NewGuid().ToString(),
                Email = request.Email,
                Name = request.Name,
                Roles = new List<string> { "Customer" }
            }
        });
    }

    public async Task<UserProfile?> GetUserProfileAsync(string userId)
    {
        // Simple demo profile
        return await Task.FromResult(new UserProfile
        {
            Id = userId,
            Username = "demo_user",
            Email = "user@example.com",
            Name = "Demo User",
            Roles = new List<string> { "Customer" },
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLogin = DateTime.UtcNow
        });
    }
}