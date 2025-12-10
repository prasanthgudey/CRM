using CRM.Server.DTOs.Auth;
using CRM.Server.Models;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;

        // ✅ Built-in: UserManager
        // ✅ Custom: IJwtTokenService
        public AuthController(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
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

            // ✅ BLOCK DEACTIVATED USERS
            if (!user.IsActive)
                return Unauthorized("Account is deactivated");

            var isValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isValid)
                return Unauthorized("Invalid credentials");

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.GenerateToken(user, roles);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(30) // Consider moving to JwtSettings later
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
    }
}
