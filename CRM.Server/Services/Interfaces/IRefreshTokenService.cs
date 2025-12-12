using CRM.Server.Models;

namespace CRM.Server.Services.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<RefreshToken> CreateRefreshTokenAsync(
            string userId,
            string ipAddress,
            string? userAgent,
            int daysValid,
            string? sessionId = null);
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task RevokeRefreshTokenAsync(RefreshToken token, string? revokedByIp = null, string? replacedByToken = null);
        Task RevokeAllRefreshTokensForUserAsync(string userId);
        Task<bool> IsRefreshTokenValidAsync(string token);
    }
}
