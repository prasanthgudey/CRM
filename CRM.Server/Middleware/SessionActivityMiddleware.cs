// CRM.Server/Middleware/SessionActivityMiddleware.cs
using CRM.Server.Security;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CRM.Server.Middleware
{
    public class SessionActivityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionActivityMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SessionSettings _settings;

        public SessionActivityMiddleware(
            RequestDelegate next,
            ILogger<SessionActivityMiddleware> logger,
            IServiceScopeFactory scopeFactory,            // <-- inject factory (root-safe)
            IOptions<SessionSettings> sessionOptions)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _settings = sessionOptions.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {

                var endpoint = context.GetEndpoint();
                if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
                {
                    await _next(context);
                    return;
                }

                var user = context.User;
                if (user?.Identity == null || !user.Identity.IsAuthenticated)
                {
                    await _next(context);
                    return;
                }

                // Create a scope per-request to resolve scoped services safely
                using var scope = _scopeFactory.CreateScope();
                var userSessionService = scope.ServiceProvider.GetRequiredService<IUserSessionService>();
                var refreshTokenService = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
                var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

                // read essential claims
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                var sessionId = user.FindFirst("session_id")?.Value;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    await _next(context);
                    return;
                }

                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    try
                    {
                        await refreshTokenService.RevokeAllRefreshTokensForUserAsync(userId);
                        await userSessionService.RevokeAllSessionsForUserAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to perform defensive revocation for user {UserId} (missing session_id)", userId);
                    }

                    await AuditAndDeny(context, auditLogService, userId, "SessionMissing", "Authentication", "Missing session_id claim");
                    return;
                }

                var session = await userSessionService.GetByIdAsync(sessionId);
                if (session == null || session.UserId != userId)
                {
                    try
                    {
                        await refreshTokenService.RevokeAllRefreshTokensForUserAsync(userId);
                        await userSessionService.RevokeAllSessionsForUserAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to revoke tokens/sessions for user {UserId} when session lookup failed", userId);
                    }

                    await AuditAndDeny(context, auditLogService, userId, "SessionNotFound", "Authentication", "Session not found or mismatch");
                    return;
                }

                if (session.IsRevoked)
                {
                    await AuditAndDeny(context, auditLogService, userId, "SessionRevoked", "Authentication", $"Session {sessionId} revoked");
                    return;
                }

                var now = DateTime.UtcNow;

                if (_settings.AbsoluteSessionLifetimeMinutes > 0 && session.ExpiresAt.HasValue)
                {
                    if (now > session.ExpiresAt.Value)
                    {
                        await userSessionService.RevokeSessionAsync(sessionId);
                        try { await refreshTokenService.RevokeAllRefreshTokensForUserAsync(userId); } catch { }

                        await AuditAndDeny(context, auditLogService, userId, "SessionAbsoluteExpired", "Authentication", $"Session {sessionId} absolute expiry reached");
                        return;
                    }
                }

                if (_settings.InactivityTimeoutMinutes > 0)
                {
                    var inactive = now - session.LastActivityAt;
                    if (inactive > TimeSpan.FromMinutes(_settings.InactivityTimeoutMinutes))
                    {
                        await userSessionService.RevokeSessionAsync(sessionId);
                        try { await refreshTokenService.RevokeAllRefreshTokensForUserAsync(userId); } catch { }

                        await AuditAndDeny(context, auditLogService, userId, "SessionExpiredByInactivity", "Authentication", $"Session {sessionId} idle {inactive.TotalSeconds}s");
                        return;
                    }
                }

                try
                {
                    await userSessionService.UpdateLastActivityAsync(sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update LastActivityAt for session {SessionId}", sessionId);
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in SessionActivityMiddleware");
                await _next(context);
            }
        }

        private static async Task AuditAndDeny(HttpContext context, IAuditLogService auditLogService, string? performedByUserId, string action, string entity, string? message)
        {
            try
            {
                await auditLogService.LogAsync(performedByUserId, null, action, entity, false, context.Connection.RemoteIpAddress?.ToString(), null, message);
            }
            catch
            {
                // swallow audit exceptions
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var payload = new { error = "session_expired", message = message ?? "Session expired or invalid" };
            await context.Response.WriteAsJsonAsync(payload);
        }
    }
}
