using CRM.Server.DTOs.Auth;
using CRM.Server.Models;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public AuthController(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            IUserService userService,
            IAuditLogService auditLogService) // ✅ ADDED
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _userService = userService;
            _auditLogService = auditLogService;
        }

        // =====================================================
        // ✅ LOGIN WITH JWT + ROLE + ACTIVE CHECK + AUDIT
        // =====================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                await SafeAudit(null, "Login Failed", "Authentication", false, ip,
                    null, $"Invalid email: {dto.Email}");

                return Unauthorized("Invalid credentials");
            }

            if (!user.IsActive)
            {
                await SafeAudit(user.Id, "Login Failed", "Authentication", false, ip,
                    null, "Account is deactivated");

                return Unauthorized("Account is deactivated");
            }

            var isValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isValid)
            {
                await SafeAudit(user.Id, "Login Failed", "Authentication", false, ip,
                    null, "Invalid password");

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

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.GenerateToken(user, roles);

            await SafeAudit(user.Id, "Login Success", "Authentication", true, ip,
                null, user.Email);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(30)
            });
        }

        // =====================================================
        // ✅ MFA LOGIN + AUDIT
        // =====================================================
        [HttpPost("mfa/login")]
        public async Task<IActionResult> MfaLogin(MfaLoginDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                await SafeAudit(null, "MFA Login Failed", "Authentication", false, ip,
                    null, "Invalid email");

                return Unauthorized("Invalid credentials");
            }

            if (!user.IsActive)
            {
                await SafeAudit(user.Id, "MFA Login Failed", "Authentication", false, ip,
                    null, "Account is deactivated");

                return Unauthorized("Account is deactivated");
            }

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                dto.Code);

            if (!isValid)
            {
                await SafeAudit(user.Id, "MFA Login Failed", "Authentication", false, ip,
                    null, "Invalid MFA code");

                return Unauthorized("Invalid verification code");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.GenerateToken(user, roles);

            await SafeAudit(user.Id, "MFA Login Success", "Authentication", true, ip,
                null, user.Email);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(30)
            });
        }

        // =====================================================
        // ✅ LOGOUT + AUDIT (NEW)
        // =====================================================
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

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
