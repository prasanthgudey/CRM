// Services/UserSessionService.cs
using CRM.Server.Data;
using CRM.Server.Models;
using CRM.Server.Security;
using CRM.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRM.Server.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UserSessionService> _logger;
        private readonly SessionSettings _sessionSettings;

        // Number of seconds to suppress frequent DB writes for last-activity updates.
        // Must be significantly smaller than InactivityTimeoutMinutes (e.g. 5-15s).
        private readonly int _writeSuppressionSeconds;

        public UserSessionService(
            ApplicationDbContext db,
            ILogger<UserSessionService> logger,
            IOptions<SessionSettings> sessionOptions)
        {
            _db = db;
            _logger = logger;
            _sessionSettings = sessionOptions.Value;

            // Choose a sensible default — 10 seconds is small enough to reflect activity
            // while still reducing high-frequency DB churn.
            _writeSuppressionSeconds = 10;
        }

        public async Task<UserSession> CreateSessionAsync(string userId, string ipAddress, string? userAgent, int? absoluteLifetimeMinutes = null)
        {
            var now = DateTime.UtcNow;

            var session = new UserSession
            {
                UserId = userId,
                CreatedAt = now,
                LastActivityAt = now,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                ExpiresAt = absoluteLifetimeMinutes.HasValue
                    ? now.AddMinutes(absoluteLifetimeMinutes.Value)
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
        /// is older than a small threshold to reduce DB churn. The threshold must be
        /// much smaller than the configured inactivity timeout.
        /// </summary>
        public async Task<bool> UpdateLastActivityAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return false;

            var session = await _db.UserSessions.SingleOrDefaultAsync(s => s.Id == sessionId);
            if (session == null || session.IsRevoked) return false;

            var now = DateTime.UtcNow;

            // If the last activity was recent, skip the DB write to reduce churn.
            // IMPORTANT: threshold must be < InactivityTimeoutMinutes * 60
            var threshold = TimeSpan.FromSeconds(_writeSuppressionSeconds);

            if (now - session.LastActivityAt < threshold)
            {
                // nothing to persist, but indicate success
                return true;
            }

            session.LastActivityAt = now;

            // OPTIONAL: If you want the absolute ExpiresAt to "slide" on activity,
            // uncomment and use the session settings to re-calculate ExpiresAt.
            // Use this only if you intentionally want the absolute lifetime to be
            // sliding — otherwise keep the hard cap behavior.
            /*
            if (_sessionSettings.AbsoluteSessionLifetimeMinutes > 0)
            {
                session.ExpiresAt = now.AddMinutes(_sessionSettings.AbsoluteSessionLifetimeMinutes);
            }
            */

            // Persist
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
            // optionally set RevokedAt, RevokedReason fields if you later extend model
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
