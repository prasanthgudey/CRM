using CRM.Server.DTOs.Auth;
using CRM.Server.Security;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ===================== LOGIN =====================
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto, HttpContext);
                return Ok(result);
            }
            catch (AuthPasswordExpiredException)
            {
                return StatusCode(403, new { error = "password_expired" });
            }
            catch (AuthMfaRequiredException ex)
            {
                return Ok(new
                {
                    mfaRequired = true,
                    email = ex.Email
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid credentials");
            }
        }


        // ===================== MFA LOGIN =====================
        [HttpPost("mfa/login")]
        [AllowAnonymous]
        public async Task<IActionResult> MfaLogin(MfaLoginDto dto)
        {
            var result = await _authService.MfaLoginAsync(dto, HttpContext);
            return Ok(result);
        }

        // ===================== REFRESH =====================
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh(RefreshRequestDto dto)
        {
            try
            {
                var result = await _authService.RefreshAsync(dto, HttpContext);
                return Ok(result);
            }
            catch (AuthTokenExpiredException)
            {
                return Unauthorized(new { error = "expired_token" });
            }
            catch
            {
                return Unauthorized(new { error = "invalid_token" });
            }
        }

        // ===================== LOGOUT =====================
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync(User, HttpContext);
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
