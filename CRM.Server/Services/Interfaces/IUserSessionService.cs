using CRM.Server.Models;

namespace CRM.Server.Services.Interfaces
{
    public interface IUserSessionService
    {
        /// <summary>
        /// Create a new user session and return it.
        /// </summary>
        Task<UserSession> CreateSessionAsync(string userId, string ipAddress, string? userAgent, int? absoluteLifetimeMinutes = null);

        /// <summary>
        /// Get session by id (returns null if not found).
        /// </summary>
        Task<UserSession?> GetByIdAsync(string sessionId);

        /// <summary>
        /// Update the session's LastActivityAt to now. Returns true if updated.
        /// </summary>
        Task<bool> UpdateLastActivityAsync(string sessionId);

        /// <summary>
        /// Revoke (invalidate) a single session.
        /// </summary>
        Task RevokeSessionAsync(string sessionId, string? reason = null);

        /// <summary>
        /// Revoke all sessions for a user (used on password change / admin).
        /// </summary>
        Task RevokeAllSessionsForUserAsync(string userId);

        Task LinkRefreshTokenToSessionAsync(string sessionId, string refreshTokenId);
        Task<UserSession?> GetByRefreshTokenIdAsync(string refreshTokenId);


    }
}
