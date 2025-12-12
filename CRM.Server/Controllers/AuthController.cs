using CRM.Server.DTOs.Auth;
using CRM.Server.Models;
using CRM.Server.Security;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService; // ✅ ADDED

        private readonly IRefreshTokenService _refreshTokenService;
        private readonly RefreshTokenSettings _refreshTokenSettings;
        private readonly JwtSettings _jwtSettings;  // add this
        private readonly IUserSessionService _userSessionService;
        private readonly SessionSettings _sessionSettings;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            IUserService userService,
            IAuditLogService auditLogService,
            IRefreshTokenService refreshTokenService,
            IOptions<RefreshTokenSettings> refreshOptions,
            IOptions<JwtSettings> jwtOptions,
            IUserSessionService userSessionService,              // NEW
            IOptions<SessionSettings> sessionOptions ,            // NEW
            ILogger<AuthController> logger
        )
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _userService = userService;
            _auditLogService = auditLogService;
            _refreshTokenService = refreshTokenService;
            _refreshTokenSettings = refreshOptions.Value;
            _jwtSettings = jwtOptions.Value;

            _userSessionService = userSessionService;
            _sessionSettings = sessionOptions.Value;
            _logger = logger;

        }


        // =====================================================
        // ✅ LOGIN WITH JWT + ROLE + ACTIVE CHECK + AUDIT
        // =====================================================
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                await SafeAudit(null, "Login Failed", "Authentication", false, ip,
                    null, null);

                return Unauthorized("Invalid credentials");
            }

            if (!user.IsActive)
            {
                await SafeAudit(user.Id, "Login Failed", "Authentication", false, ip,
                    null,null);

                return Unauthorized("Account is deactivated");
            }

            var isValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isValid)
            {
                await SafeAudit(user.Id, "Login Failed", "Authentication", false, ip,
                    null, null);

                return Unauthorized("Invalid credentials");
            }

            // ✅ MFA CHECK
            if (user.TwoFactorEnabled)
            {
                return Ok(new AuthResponseDto
                {
                    MfaRequired = true,
                    Email = user.Email
                });
            }

            // NEW: check for password expiry via service
            if (await _userService.IsPasswordExpiredAsync(user))
            {
                // Audit the expired-login attempt (use existing SafeAudit helper)
                await SafeAudit(user.Id, "Login Failed - Password Expired", "Authentication", false, ip,
                    null, null);

                // Return 403 with a clear payload the client can detect
                return StatusCode(403, new { error = "password_expired", message = "Password expired. Please change your password." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            // AFTER password check and MFA handling and expiry checks

      
            var userAgent = Request.Headers["User-Agent"].ToString();

            // Pass absolute lifetime minutes if you want; otherwise pass null to leave ExpiresAt null
            int? absoluteLifetime = _sessionSettings.AbsoluteSessionLifetimeMinutes > 0
                ? _sessionSettings.AbsoluteSessionLifetimeMinutes
                : (int?)null;

            var session = await _userSessionService.CreateSessionAsync(user.Id, ip, userAgent, absoluteLifetime);

            // 2) generate JWT including session id
            var token = _jwtTokenService.GenerateToken(user, roles, session.Id);

            // 3) create refresh token and persist
            var refresh = await _refreshTokenService.CreateRefreshTokenAsync(user.Id, ip, userAgent, _refreshTokenSettings.RefreshTokenExpiryDays,session.Id);

            // 4) link refresh token id to session
            await _userSessionService.LinkRefreshTokenToSessionAsync(session.Id, refresh.Id);

            // 5) audit and return
            await SafeAudit(user.Id, "Login Success", "Authentication", true, ip, null, null);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                RefreshToken = refresh.Token,
                RefreshExpiresAt = refresh.ExpiresAt
            });

        }



        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshRequestDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var incomingToken = dto.RefreshToken?.Trim();

            if (string.IsNullOrEmpty(incomingToken))
                return Unauthorized(new { error = "invalid_token" });

            // 1) Look up refresh token record
            var stored = await _refreshTokenService.GetByTokenAsync(incomingToken);
            if (stored == null)
            {
                _logger.LogInformation("Refresh token not found for token: {TokenPrefix}...", incomingToken?.Substring(0, Math.Min(8, incomingToken?.Length ?? 0)));
                return Unauthorized(new { error = "invalid_token" });
            }

            // 2) If token is revoked -> treat as reuse; revoke all tokens & sessions for user and deny
            if (stored.IsRevoked)
            {
                _logger.LogWarning("Refresh token reuse detected. RefreshId={RefreshId} UserId={UserId}", stored.Id, stored.UserId);
                await _refreshTokenService.RevokeAllRefreshTokensForUserAsync(stored.UserId);
                try { await _userSessionService.RevokeAllSessionsForUserAsync(stored.UserId); } catch { /* swallow */ }

                return Unauthorized(new { error = "invalid_token" });
            }

            // 3) If token expired -> deny
            if (stored.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogInformation("Refresh token expired. RefreshId={RefreshId} UserId={UserId}", stored.Id, stored.UserId);
                return Unauthorized(new { error = "expired_token" });
            }

            // 4) Load user and validate
            var user = await _userManager.FindByIdAsync(stored.UserId);
            if (user == null || !user.IsActive)
            {
                _logger.LogInformation("Refresh token user invalid or inactive. RefreshId={RefreshId} UserId={UserId}", stored.Id, stored.UserId);
                // revoke the single token defensively
                await _refreshTokenService.RevokeRefreshTokenAsync(stored, ip, null);
                try { await _userSessionService.RevokeAllSessionsForUserAsync(stored.UserId); } catch { }
                return Unauthorized(new { error = "invalid_user" });
            }

            // 4.5) Determine session id associated with this refresh token
            string? sessionId = stored.SessionId; // prefer SessionId stored on the token

            // If not present on token, try to find by service (backward-compat)
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                var linkedSession = await _userSessionService.GetByRefreshTokenIdAsync(stored.Id);
                sessionId = linkedSession?.Id;
            }

            // If no session found, REVOKE only the single refresh token and return unauthorized.
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                // Safer than revoking everything: revoke this token and log details for investigation
                await _refreshTokenService.RevokeRefreshTokenAsync(stored, ip, null);
                _logger.LogWarning("Refresh token found with no linked session. RefreshId={RefreshId} UserId={UserId}", stored.Id, stored.UserId);

                return Unauthorized(new { error = "invalid_token" });
            }

            // 5) Generate new access token including session id
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _jwtTokenService.GenerateToken(user, roles, sessionId);

            // 6) Rotation: create new refresh token and revoke old (if rotation enabled)
            string? returnedRefreshToken = null;
            DateTime? returnedRefreshExpires = null;

            if (_refreshTokenSettings.RotationEnabled)
            {
                var userAgent = Request.Headers["User-Agent"].ToString();

                // create replacement token and store sessionId on it
                var replacement = await _refreshTokenService.CreateRefreshTokenAsync(
                    user.Id, ip, userAgent, _refreshTokenSettings.RefreshTokenExpiryDays, sessionId);

                // revoke old and link to replacement (ReplacedByToken stored in old token row)
                await _refreshTokenService.RevokeRefreshTokenAsync(stored, ip, replacement.Token);

                // Link the new refresh token to the same session row (optional if CreateRefreshToken already set SessionId)
                try { await _userSessionService.LinkRefreshTokenToSessionAsync(sessionId, replacement.Id); } catch { /* swallow */ }

                returnedRefreshToken = replacement.Token;
                returnedRefreshExpires = replacement.ExpiresAt;
            }
            else
            {
                // no rotation — continue using existing token
                returnedRefreshToken = stored.Token;
                returnedRefreshExpires = stored.ExpiresAt;
            }

            _logger.LogInformation("Refresh successful. UserId={UserId} SessionId={SessionId} RefreshId={RefreshId}", stored.UserId, sessionId, stored.Id);

            return Ok(new AuthResponseDto
            {
                Token = newAccessToken,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                RefreshToken = returnedRefreshToken,
                RefreshExpiresAt = returnedRefreshExpires
            });
        }



        // =====================================================
        // ✅ MFA LOGIN + AUDIT
        // =====================================================
        [HttpPost("mfa/login")]
        public async Task<IActionResult> MfaLogin(MfaLoginDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                await SafeAudit(null, "MFA Login Failed", "Authentication", false, ip,
                    null, null);

                return Unauthorized("Invalid credentials");
            }

            if (!user.IsActive)
            {
                await SafeAudit(user.Id, "MFA Login Failed", "Authentication", false, ip,
                    null, null);

                return Unauthorized("Account is deactivated");
            }

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                dto.Code);

            if (!isValid)
            {
                await SafeAudit(user.Id, "MFA Login Failed", "Authentication", false, ip,
                    null, null);

                return Unauthorized("Invalid verification code");
            }

            // --- create session ---
            int? absoluteLifetime = _sessionSettings.AbsoluteSessionLifetimeMinutes > 0
                ? _sessionSettings.AbsoluteSessionLifetimeMinutes
                : (int?)null;

            var session = await _userSessionService.CreateSessionAsync(user.Id, ip, userAgent, absoluteLifetime);

            // --- generate access token that includes the session id ---
            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.GenerateToken(user, roles, session.Id);

            // --- create refresh token and link to session ---
            var refresh = await _refreshTokenService.CreateRefreshTokenAsync(
                user.Id, ip, userAgent, _refreshTokenSettings.RefreshTokenExpiryDays,session.Id);

            await _userSessionService.LinkRefreshTokenToSessionAsync(session.Id, refresh.Id);

            await SafeAudit(user.Id, "MFA Login Success", "Authentication", true, ip, null, null);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                RefreshToken = refresh.Token,
                RefreshExpiresAt = refresh.ExpiresAt
            });
        }

        // =====================================================
        // ✅ FORGOT PASSWORD  
        // =====================================================


        // DTOs assumed to exist in CRM.Server.DTOs.Auth:
        // public class ForgotPasswordDto { public string Email { get; set; } }
        // public class ResetPasswordDto { public string Email { get; set; } public string Token { get; set; } public string NewPassword { get; set; } }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                // The service handles "no user found" silently to avoid enumeration
                await _userService.ForgotPasswordAsync(dto.Email);

                // Audit the request (do not include whether user existed)
                //await SafeAudit(null, "Forgot Password Requested", "Authentication", true, ip, null, dto.Email);

                // Generic response
                return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
            }
            catch (Exception ex)
            {
                // Audit failure but still return same generic message to avoid leaking info
                //try { await SafeAudit(null, "Forgot Password Failed", "Authentication", false, ip, null, ex.Message); } catch { }

                return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
            }
        }


        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            try
            {
                // 1) Perform the reset (service updates PasswordLastChanged)
                await _userService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);

                // 2) Load user; if not found, treat as generic failure (avoid enumeration)
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    await SafeAudit(null, "Password Reset Failed", "Authentication", false, ip, null, null);
                    // Return generic message
                    return BadRequest(new { error = "invalid_request", message = "Could not complete request." });
                }

                // 3) Revoke all existing refresh tokens & sessions for security (optional but recommended)
                try
                {
                    await _refreshTokenService.RevokeAllRefreshTokensForUserAsync(user.Id);
                    await _userSessionService.RevokeAllSessionsForUserAsync(user.Id);
                }
                catch
                {
                    // swallow — don't block the flow if cleanup fails
                }

                // 4) Create a new session
                int? absoluteLifetime = _sessionSettings.AbsoluteSessionLifetimeMinutes > 0
                    ? _sessionSettings.AbsoluteSessionLifetimeMinutes
                    : (int?)null;

                var session = await _userSessionService.CreateSessionAsync(user.Id, ip, userAgent, absoluteLifetime);

                // 5) Generate JWT including session id
                var roles = await _userManager.GetRolesAsync(user);
                var token = _jwtTokenService.GenerateToken(user, roles, session.Id);

                // 6) Create refresh token and link it to the session
                var refresh = await _refreshTokenService.CreateRefreshTokenAsync(user.Id, ip, userAgent, _refreshTokenSettings.RefreshTokenExpiryDays, session.Id);
                await _userSessionService.LinkRefreshTokenToSessionAsync(session.Id, refresh.Id);

                // 7) Audit
                await SafeAudit(user.Id, "Password Reset (Token)", "Authentication", true, ip, null, null);

                // 8) Return tokens
                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                    RefreshToken = refresh.Token,
                    RefreshExpiresAt = refresh.ExpiresAt
                });
            }
            catch (Exception ex)
            {
                // Audit failure (do not reveal details to client)
                try { await SafeAudit(null, "Password Reset Failed", "Authentication", false, ip, null, null); } catch { }

                // Generic error response to avoid token/username enumeration or leaking internal errors
                return BadRequest(new { error = "invalid_request", message = "Invalid token or request." });
            }
        }








        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Try to get session id from token (if present)
            var sessionId = User.FindFirst("session_id")?.Value;

            try
            {
                if (!string.IsNullOrEmpty(sessionId))
                {
                    // Revoke the current session
                    await _userSessionService.RevokeSessionAsync(sessionId);

                    // If the session has a linked refresh token, revoke that as well
                    var session = await _userSessionService.GetByIdAsync(sessionId);
                    if (session != null && !string.IsNullOrWhiteSpace(session.RefreshTokenId))
                    {
                        var rt = await _refreshTokenService.GetByTokenAsync(session.RefreshTokenId);
                        // If your RefreshTokenService supports lookup by id, use that instead.
                        if (rt != null)
                        {
                            await _refreshTokenService.RevokeRefreshTokenAsync(rt, ip, null);
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(userId))
                {
                    // No session id available — fall back to revoking all refresh tokens for the user
                    await _refreshTokenService.RevokeAllRefreshTokensForUserAsync(userId);
                    // Also revoke all sessions as defensive cleanup
                    await _userSessionService.RevokeAllSessionsForUserAsync(userId);
                }
            }
            catch
            {
                // don't throw to the client on cleanup errors — still try to audit and return success
            }

            await SafeAudit(userId, "Logout", "Authentication", true, ip, null, null);

            return Ok(new { message = "Logged out successfully" });
        }



        // =====================================================
        // ✅ CHANGE PASSWORD + AUDIT
        // =====================================================
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _userService.ChangePasswordAsync(
                userId!, dto.CurrentPassword, dto.NewPassword);

            await SafeAudit(userId, "Password Changed", "Authentication", true, ip, null, null);

            return Ok(new { message = "Password changed successfully" });
        }


        [HttpPost("change-expired-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ChangeExpiredPassword(ChangeExpiredPasswordDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            try
            {
                // 1) Perform the password change (verifies old password)
                await _userService.ChangePasswordByEmailAsync(dto.Email, dto.CurrentPassword, dto.NewPassword);

                // 2) Re-fetch user to create a token
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    await SafeAudit(null, "Password Change Failed (Expired Flow)", "Authentication", false, ip, null,null);
                    return BadRequest(new { error = "invalid_request", message = "Could not complete request." });
                }

                // 3) Revoke all existing refresh tokens for the user (security: invalidate previous sessions/tokens)
                await _refreshTokenService.RevokeAllRefreshTokensForUserAsync(user.Id);

                // 4) Create a new server-side session
                int? absoluteLifetime = _sessionSettings.AbsoluteSessionLifetimeMinutes > 0
                    ? _sessionSettings.AbsoluteSessionLifetimeMinutes
                    : (int?)null;

                var session = await _userSessionService.CreateSessionAsync(user.Id, ip, userAgent, absoluteLifetime);

                // 5) Generate JWT including session id
                var roles = await _userManager.GetRolesAsync(user);
                var token = _jwtTokenService.GenerateToken(user, roles, session.Id);

                // 6) Create a new refresh token and link it to the session
                var refresh = await _refreshTokenService.CreateRefreshTokenAsync(user.Id, ip, userAgent, _refreshTokenSettings.RefreshTokenExpiryDays, session.Id);
                await _userSessionService.LinkRefreshTokenToSessionAsync(session.Id, refresh.Id);

                // 7) Audit success
                await SafeAudit(user.Id, "Password Changed (Expired Flow)", "Authentication", true, ip, null, null);

                // 8) Return tokens to client
                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                    RefreshToken = refresh.Token,
                    RefreshExpiresAt = refresh.ExpiresAt
                });
            }
            catch (Exception ex)
            {
                // Audit failure / invalid credentials
                await SafeAudit(null, "Password Change Failed (Expired Flow)", "Authentication", false, ip, null, null);

                // Return a generic message to avoid user enumeration
                return BadRequest(new { error = "invalid_request", message = "Invalid credentials or request." });
            }
        }



        // =====================================================
        // ✅ MFA ENABLE / VERIFY / DISABLE + AUDIT
        // =====================================================
        [HttpPost("mfa/enable")]
        [Authorize]
        public async Task<IActionResult> EnableMfa()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _userService.EnableMfaAsync(userId!);

            await SafeAudit(userId, "MFA Enabled", "Authentication", true, ip, null, null);

            return Ok(result);
        }

        [HttpPost("mfa/verify")]
        [Authorize]
        public async Task<IActionResult> VerifyMfa(VerifyMfaDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _userService.VerifyMfaAsync(userId!, dto.Code);

            await SafeAudit(userId, "MFA Verified", "Authentication", true, ip, null, null);

            return Ok(new { message = "MFA enabled successfully" });
        }

        [HttpPost("mfa/disable")]
        [Authorize]
        public async Task<IActionResult> DisableMfa(VerifyMfaDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _userService.DisableMfaAsync(userId!, dto.Code);

            await SafeAudit(userId, "MFA Disabled", "Authentication", true, ip, null, null);

            return Ok(new { message = "MFA disabled successfully" });
        }

        // =====================================================
        // ✅ SAFE AUDIT WRAPPER (PROTECTS AUTH FLOW)
        // =====================================================
        private async Task SafeAudit(
            string? performedByUserId,
            string action,
            string entityName,
            bool isSuccess,
            string? ipAddress,
            string? oldValue,
            string? newValue)
        {
            try
            {
                await _auditLogService.LogAsync(
                    performedByUserId,
                    null,
                    action,
                    entityName,
                    isSuccess,
                    ipAddress,
                    oldValue,
                    newValue
                );
            }
            catch
            {
                // ✅ Never allow audit failure to break authentication
            }
        }
    }
}
