namespace ChatbotService.Services.Interfaces;

using ChatbotService.Models;

public interface IAuthenticationService
{
    Task<AuthResponse> AuthenticateAsync(AuthRequest request);
    Task<AuthResponse> AuthenticateAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task<bool> ValidateTokenAsync(string token);
    Task<UserInfo?> GetUserInfoAsync(string userId);
    Task<UserProfile?> GetUserProfileAsync(string userId);
    string GenerateToken(UserInfo user);
    string GenerateRefreshToken();
}