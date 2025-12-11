using CRM.Server.Data;
using CRM.Server.Models;
using CRM.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRM.Server.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly ApplicationDbContext _db;

        public UserSessionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<UserSession> CreateSessionAsync(string userId, string ipAddress, string? userAgent, int? absoluteLifetimeMinutes = null)
        {
            var session = new UserSession
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                ExpiresAt = absoluteLifetimeMinutes.HasValue
                    ? DateTime.UtcNow.AddMinutes(absoluteLifetimeMinutes.Value)
                    : (DateTime?)null,
                IsRevoked = false
            };

            _db.UserSessions.Add(session);
            await _db.SaveChangesAsync();
            return session;
        }

        public async Task<UserSession?> GetByIdAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return null;
            return await _db.UserSessions
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.Id == sessionId);
        }

        /// <summary>
        /// Updates LastActivityAt to now, but only writes to DB if the previous LastActivityAt
        /// is older than a small threshold (to reduce DB churn).
        /// </summary>
        public async Task<bool> UpdateLastActivityAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return false;

            var session = await _db.UserSessions.SingleOrDefaultAsync(s => s.Id == sessionId);
            if (session == null || session.IsRevoked) return false;

            var now = DateTime.UtcNow;

            // Avoid frequent writes: only update if last activity is older than 1 minute.
            if (now - session.LastActivityAt < TimeSpan.FromSeconds(60))
                return true;

            session.LastActivityAt = now;
            _db.UserSessions.Update(session);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task RevokeSessionAsync(string sessionId, string? reason = null)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return;

            var session = await _db.UserSessions.SingleOrDefaultAsync(s => s.Id == sessionId);
            if (session == null) return;

            if (session.IsRevoked) return;

            session.IsRevoked = true;
            // you can add a RevokedAt if you later extend the model
            await _db.SaveChangesAsync();
        }

        public async Task RevokeAllSessionsForUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            var sessions = await _db.UserSessions
                .Where(s => s.UserId == userId && !s.IsRevoked)
                .ToListAsync();

            if (!sessions.Any()) return;

            foreach (var s in sessions)
            {
                s.IsRevoked = true;
            }

            await _db.SaveChangesAsync();
        }

        public async Task LinkRefreshTokenToSessionAsync(string sessionId, string refreshTokenId)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(refreshTokenId)) return;

            var session = await _db.UserSessions.SingleOrDefaultAsync(s => s.Id == sessionId);
            if (session == null) return;

            session.RefreshTokenId = refreshTokenId;
            _db.UserSessions.Update(session);
            await _db.SaveChangesAsync();
        }


        public async Task<UserSession?> GetByRefreshTokenIdAsync(string refreshTokenId)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenId)) return null;
            return await _db.UserSessions
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.RefreshTokenId == refreshTokenId);
        }

    }
}
