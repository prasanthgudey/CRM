
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

        // ✅ Built-in: UserManager
        // ✅ Custom: IJwtTokenService
        public AuthController(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService, IUserService userService)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _userService = userService;
        }

        // =====================================================
        // ✅ LOGIN WITH JWT + ROLE + ACTIVE CHECK
        // =====================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized("Invalid credentials");

            if (!user.IsActive)
                return Unauthorized("Account is deactivated");

            var isValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isValid)
                return Unauthorized("Invalid credentials");

            // ✅✅✅ MFA CHECK (CRITICAL)
            if (user.TwoFactorEnabled)
            {
                return Ok(new AuthResponseDto
                {
                    MfaRequired = true,
                    Email = user.Email
                });
            }

            // ✅ NORMAL LOGIN (NO MFA)
            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.GenerateToken(user, roles);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(30)
            });
        }


        // =====================================================
        // ✅ COMPLETE REGISTRATION FROM INVITE LINK (SECURE)
        // =====================================================
        [HttpPost("complete-registration")]
        public async Task<IActionResult> CompleteRegistration(CompleteRegistrationDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null || !user.IsInvitePending)
                return BadRequest("Invalid or already completed invite");

            // ✅ CHECK INVITE EXPIRY
            if (user.InviteExpiry.HasValue && user.InviteExpiry < DateTime.UtcNow)
                return BadRequest("Invitation link has expired");

            // ✅ IMPORTANT FIX: URL DECODE TOKEN
            var decodedToken = System.Net.WebUtility.UrlDecode(dto.Token);

            var result = await _userManager.ResetPasswordAsync(
                 user, decodedToken, dto.Password);


            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // ✅ ACTIVATE ACCOUNT
            user.IsInvitePending = false;
            user.IsActive = true;
            user.InviteExpiry = null;

            await _userManager.UpdateAsync(user);

            // ⭐ new code – JSON success instead of plain string
            return Ok(new
            {
                success = true,
                message = "Registration completed successfully"
            });
        }

        // =====================================================
        // ✅ FORGOT PASSWORD
        // =====================================================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            await _userService.ForgotPasswordAsync(dto.Email);
            return Ok(new { message = "If the email is registered, a reset link has been sent." });
        }

        // =====================================================
        // ✅ RESET PASSWORD
        // =====================================================
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            await _userService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
            return Ok(new { message = "Password reset successful" });
        }

        // =====================================================
        // ✅ CHANGE PASSWORD (LOGGED-IN USER)
        // =====================================================
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _userService.ChangePasswordAsync(
                userId!, dto.CurrentPassword, dto.NewPassword);

            return Ok(new { message = "Password changed successfully" });
        }

        [HttpPost("mfa/enable")]
        [Authorize]
        public async Task<IActionResult> EnableMfa()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _userService.EnableMfaAsync(userId!);

            return Ok(result);
        }

        [HttpPost("mfa/verify")]
        [Authorize]
        public async Task<IActionResult> VerifyMfa(VerifyMfaDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _userService.VerifyMfaAsync(userId!, dto.Code);

            return Ok(new { message = "MFA enabled successfully" });
        }

        [HttpPost("mfa/disable")]
        [Authorize]
        public async Task<IActionResult> DisableMfa(VerifyMfaDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _userService.DisableMfaAsync(userId!, dto.Code);

            return Ok(new { message = "MFA disabled successfully" });
        }

        [HttpPost("mfa/login")]
        public async Task<IActionResult> MfaLogin(MfaLoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized("Invalid credentials");

            if (!user.IsActive)
                return Unauthorized("Account is deactivated");

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                dto.Code);

            if (!isValid)
                return Unauthorized("Invalid verification code");

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.GenerateToken(user, roles);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(30)
            });
        }

    }
}
