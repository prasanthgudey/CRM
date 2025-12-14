using CRM.Server.DTOs.Auth;
using CRM.Server.DTOs.Users;
using CRM.Server.Models;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            IUserService userService,
            IAuditLogService auditLogService,
            UserManager<ApplicationUser> userManager)
        {
            _userService = userService;
            _auditLogService = auditLogService;
            _userManager = userManager;
        }

        // ===================== INVITE USER =====================
        [Authorize]
        [HttpPost("invite")]
        public async Task<IActionResult> InviteUser(InviteUserDto dto)
        {
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.InviteUserAsync(dto, performedBy!);

            return Ok(new { success = true, message = "Invitation sent successfully" });
        }

        // ===================== COMPLETE REGISTRATION =====================
        [AllowAnonymous]
        [HttpPost("complete-registration")]
        public async Task<IActionResult> CompleteRegistration(CompleteRegistrationDto dto)
        {
            await _userService.CompleteRegistrationAsync(dto);

            return Ok(new { success = true, message = "Registration completed successfully" });
        }

        // ===================== FORGOT PASSWORD =====================
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            await _userService.ForgotPasswordAsync(dto.Email);

            return Ok(new
            {
                message = "If an account with that email exists, a password reset link has been sent."
            });
        }

        // ===================== RESET PASSWORD =====================
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            await _userService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);

            return Ok(new { message = "Password reset successfully" });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                await _userService.ChangePasswordAsync(
                    userId!, dto.CurrentPassword, dto.NewPassword);

                return Ok(new { message = "Password changed successfully" });
            }
            catch (InvalidOperationException ex)
            {
           
                return BadRequest(new { message = ex.Message });
            }
        }


        // ===================== CHANGE EXPIRED PASSWORD =====================
        [AllowAnonymous]
        [HttpPost("change-expired-password")]
        public async Task<IActionResult> ChangeExpiredPassword(ChangeExpiredPasswordDto dto)
        {
            try
            {
                await _userService.ChangePasswordByEmailAsync(
                    dto.Email, dto.CurrentPassword, dto.NewPassword);

                return Ok(new { message = "Password updated successfully" });
            }
            catch (InvalidOperationException ex)
            {
               
                return BadRequest(new { message = ex.Message });
            }
        }


        // ===================== MFA ENABLE =====================
        [Authorize]
        [HttpPost("mfa/enable")]
        public async Task<IActionResult> EnableMfa()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _userService.EnableMfaAsync(userId!);
            return Ok(result);
        }

        // ===================== MFA VERIFY =====================
        [Authorize]
        [HttpPost("mfa/verify")]
        public async Task<IActionResult> VerifyMfa(VerifyMfaDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.VerifyMfaAsync(userId!, dto.Code);

            return Ok(new { message = "MFA enabled successfully" });
        }

        // ===================== MFA DISABLE =====================
        [Authorize]
        [HttpPost("mfa/disable")]
        public async Task<IActionResult> DisableMfa(VerifyMfaDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.DisableMfaAsync(userId!, dto.Code);

            return Ok(new { message = "MFA disabled successfully" });
        }
    }
}
