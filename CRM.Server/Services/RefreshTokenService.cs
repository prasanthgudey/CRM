using CRM.Server.Data;
using CRM.Server.Models;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CRM.Server.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly ApplicationDbContext _db;

        public RefreshTokenService(ApplicationDbContext db)
        {
            _db = db;
        }

        // file: Services/RefreshTokenService.cs
        public async Task<RefreshToken> CreateRefreshTokenAsync(
            string userId,
            string ipAddress,
            string? userAgent,
            int daysValid,
            string? sessionId = null)   // <-- new optional parameter
        {
            var tokenString = GenerateSecureToken();

            var refresh = new RefreshToken
            {
                UserId = userId,
                Token = tokenString,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(daysValid),
                CreatedByIp = ipAddress,
                UserAgent = userAgent,
                SessionId = sessionId          // <-- store the session id
            };

            _db.RefreshTokens.Add(refresh);
            await _db.SaveChangesAsync();

            return refresh;
        }


        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            return await _db.RefreshTokens
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Token == token);
        }

        public async Task<bool> IsRefreshTokenValidAsync(string token)
        {
            var rt = await _db.RefreshTokens
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Token == token);

            if (rt == null) return false;
            if (rt.IsRevoked) return false;
            if (rt.ExpiresAt <= DateTime.UtcNow) return false;
            return true;
        }

        public async Task RevokeRefreshTokenAsync(RefreshToken token, string? revokedByIp = null, string? replacedByToken = null)
        {
            if (token == null) return;

            var existing = await _db.RefreshTokens.SingleOrDefaultAsync(x => x.Id == token.Id);
            if (existing == null) return;

            existing.IsRevoked = true;
            existing.RevokedAt = DateTime.UtcNow;
            existing.ReplacedByToken = replacedByToken;

            await _db.SaveChangesAsync();
        }

        public async Task RevokeAllRefreshTokensForUserAsync(string userId)
        {
            var tokens = await _db.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

            if (!tokens.Any()) return;

            foreach (var t in tokens)
            {
                t.IsRevoked = true;
                t.RevokedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }

        // Secure random token generator, url-safe
        private static string GenerateSecureToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return WebEncoders.Base64UrlEncode(bytes);
        }
    }
}
