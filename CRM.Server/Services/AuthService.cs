using CRM.Server.DTOs.Auth;
using CRM.Server.Models;
using CRM.Server.Security;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
namespace CRM.Server.Services
{
    public class AuthService : IAuthService
    {
        // ---------- Dependencies ----------
        // UserManager<ApplicationUser>  => FRAMEWORK (Identity)
        // IJwtTokenService              => USER-DEFINED
        // IUserService                  => USER-DEFINED
        // IRefreshTokenService          => USER-DEFINED
        // IUserSessionService           => USER-DEFINED
        // IAuditLogService              => USER-DEFINED

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IUserService _userService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IUserSessionService _userSessionService;
        private readonly IAuditLogService _auditLogService;
        private readonly JwtSettings _jwtSettings;
        private readonly RefreshTokenSettings _refreshTokenSettings;
        private readonly SessionSettings _sessionSettings;
        private readonly ILogger<AuthService> _logger;
     
        public AuthService(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            IUserService userService,
            IRefreshTokenService refreshTokenService,
            IUserSessionService userSessionService,
            IAuditLogService auditLogService,
            IOptions<JwtSettings> jwtOptions,
            IOptions<RefreshTokenSettings> refreshOptions,
            IOptions<SessionSettings> sessionOptions,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _userService = userService;
            _refreshTokenService = refreshTokenService;
            _userSessionService = userSessionService;
            _auditLogService = auditLogService;
            _jwtSettings = jwtOptions.Value;
            _refreshTokenSettings = refreshOptions.Value;
            _sessionSettings = sessionOptions.Value;
            _logger = logger;
        }

        // =====================================================
        // LOGIN (JWT + SESSION + REFRESH TOKEN)
        public async Task<AuthResponseDto> LoginAsync(
       LoginRequestDto dto,
       HttpContext httpContext)
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

            // 1️⃣ Find user
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("Invalid credentials");

            // 2️⃣ Verify password
            var isValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isValid)
                throw new UnauthorizedAccessException("Invalid credentials");

            // 3️⃣ Password expired
            if (await _userService.IsPasswordExpiredAsync(user))
                throw new AuthPasswordExpiredException();

            // 4️⃣ MFA required
            if (user.TwoFactorEnabled)
                throw new AuthMfaRequiredException(user.Email!);

            // 5️⃣ Roles
            var roles = await _userManager.GetRolesAsync(user);

            // 6️⃣ Session lifetime
            int? absoluteLifetime =
                _sessionSettings.AbsoluteSessionLifetimeMinutes > 0
                    ? _sessionSettings.AbsoluteSessionLifetimeMinutes
                    : null;

            // 7️⃣ Create session
            var session = await _userSessionService.CreateSessionAsync(
                user.Id,
                ip,
                userAgent,
                absoluteLifetime);

            // 8️⃣ Generate JWT
            var token = _jwtTokenService.GenerateToken(
                user,
                roles,
                session.Id);

            // 9️⃣ Create refresh token
            var refresh = await _refreshTokenService.CreateRefreshTokenAsync(
                user.Id,
                ip,
                userAgent,
                _refreshTokenSettings.RefreshTokenExpiryDays,
                session.Id);

            await _userSessionService.LinkRefreshTokenToSessionAsync(
                session.Id,
                refresh.Id);

            // 🔟 Audit
            await _auditLogService.LogAsync(
                user.Id,
                null,
                "Login Success",
                "Authentication",
                true,
                ip,
                null,
                null);

            // ✅ Success
            return new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                RefreshToken = refresh.Token,
                RefreshExpiresAt = refresh.ExpiresAt
            };
        }


        public async Task<AuthResponseDto> MfaLoginAsync(
    MfaLoginDto dto,
    HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("Invalid credentials");

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                dto.Code);

            if (!isValid)
                throw new UnauthorizedAccessException("Invalid verification code");

            var roles = await _userManager.GetRolesAsync(user);

            int? absoluteLifetime =
                _sessionSettings.AbsoluteSessionLifetimeMinutes > 0
                    ? _sessionSettings.AbsoluteSessionLifetimeMinutes
                    : null;

            // ✅ Create session
            var session = await _userSessionService.CreateSessionAsync(
                user.Id, ip, userAgent, absoluteLifetime);

            // ✅ Generate JWT with session id
            var accessToken = _jwtTokenService.GenerateToken(user, roles, session.Id);

            // ✅ Create refresh token
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(
                user.Id,
                ip,
                userAgent,
                _refreshTokenSettings.RefreshTokenExpiryDays,
                session.Id);

            await _userSessionService.LinkRefreshTokenToSessionAsync(
                session.Id, refreshToken.Id);

            // ✅ Audit
            await _auditLogService.LogAsync(
                user.Id,
                null,
                "MFA Login Success",
                "Authentication",
                true,
                ip,
                null,
                null);

            return new AuthResponseDto
            {
                Token = accessToken,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                RefreshToken = refreshToken.Token,
                RefreshExpiresAt = refreshToken.ExpiresAt
            };
        }

        public async Task<AuthResponseDto> RefreshAsync(
     RefreshRequestDto dto,
     HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            //  Get refresh token from DB
            var stored = await _refreshTokenService.GetByTokenAsync(dto.RefreshToken);
            if (stored == null ||
                stored.IsRevoked ||
                stored.ExpiresAt <= DateTime.UtcNow)
            {
                throw new AuthInvalidTokenException();
            }

            //  Validate user
            var user = await _userManager.FindByIdAsync(stored.UserId);
            if (user == null || !user.IsActive)
            {
                throw new AuthInvalidTokenException();
            }

            //  Resolve session id (preferred: from refresh token)
            var sessionId = stored.SessionId
                ?? (await _userSessionService.GetByRefreshTokenIdAsync(stored.Id))?.Id;

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new AuthInvalidTokenException();
            }

            //  Load session and ENFORCE session ↔ refresh token match
            var session = await _userSessionService.GetByIdAsync(sessionId);

            if (session == null ||
                session.IsRevoked ||
                session.ExpiresAt <= DateTime.UtcNow ||
                session.RefreshTokenId != stored.Id)
            {
                throw new AuthInvalidTokenException();
            }

            //  Generate new access token
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _jwtTokenService.GenerateToken(user, roles, sessionId);

            //  Create rotated refresh token
            var replacement = await _refreshTokenService.CreateRefreshTokenAsync(
                user.Id,
                ip,
                userAgent,
                _refreshTokenSettings.RefreshTokenExpiryDays,
                sessionId
            );

            //  Update session FIRST
            await _userSessionService.LinkRefreshTokenToSessionAsync(
                sessionId,
                replacement.Id
            );

            //  Revoke old refresh token
            await _refreshTokenService.RevokeRefreshTokenAsync(
                stored,
                ip,
                replacement.Token
            );

            //  Return response
            return new AuthResponseDto
            {
                Token = accessToken,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                RefreshToken = replacement.Token,
                RefreshExpiresAt = replacement.ExpiresAt
            };
        }



        public async Task LogoutAsync(
    ClaimsPrincipal user,
    HttpContext context)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var ip = context.Connection.RemoteIpAddress?.ToString();

        // Try to get session id from JWT
        var sessionId = user.FindFirst("session_id")?.Value;

        try
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                // Revoke current session
                await _userSessionService.RevokeSessionAsync(sessionId);

                // If session has linked refresh token → revoke it
                var session = await _userSessionService.GetByIdAsync(sessionId);
                if (session != null && !string.IsNullOrWhiteSpace(session.RefreshTokenId))
                {
                    // ⚠️ If RefreshTokenId is actually an ID, use GetByIdAsync instead
                    var refreshToken = await _refreshTokenService
                        .GetByTokenAsync(session.RefreshTokenId);

                    if (refreshToken != null)
                    {
                        await _refreshTokenService
                            .RevokeRefreshTokenAsync(refreshToken, ip, null);
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(userId))
            {
                // Fallback: revoke all user sessions & refresh tokens
                await _refreshTokenService
                    .RevokeAllRefreshTokensForUserAsync(userId);

                await _userSessionService
                    .RevokeAllSessionsForUserAsync(userId);
            }
        }
        catch
        {
            // Swallow cleanup errors — logout should never fail
        }

        // Audit (never throw)
        try
        {
            await _auditLogService.LogAsync(
                userId,
                null,
                "Logout",
                "Authentication",
                true,
                ip,
                null,
                null);
        }
        catch { }
    }

}
}
