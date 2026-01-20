using UserService.Models;

namespace UserService.Services;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task SaveRefreshTokenAsync(RefreshToken refreshToken);
    Task RevokeRefreshTokenAsync(string token, string? ipAddress);
    Task RevokeAllUserTokensAsync(Guid userId);
    Task CleanupExpiredTokensAsync();
}
