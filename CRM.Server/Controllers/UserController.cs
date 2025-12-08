using CRM.Server.DTOs.Users;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // ✅ Admin only
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        // ✅ Custom service
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // =====================================================
        // ✅ MANUAL USER CREATION (TEMP PASSWORD)
        // =====================================================
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _userService.CreateUserAsync(dto);
                return Ok(new { message = "User created successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // =====================================================
        // ✅ GET ALL USERS
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        // =====================================================
        // ✅ INVITE USER (EMAIL REGISTRATION LINK)
        // =====================================================
        [HttpPost("invite")]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _userService.InviteUserAsync(dto);
                return Ok(new { message = "Invitation sent successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // =====================================================
        // ✅ DEACTIVATE USER
        // =====================================================
        [HttpPut("deactivate/{userId}")]
        public async Task<IActionResult> Deactivate(string userId)
        {
            try
            {
                await _userService.DeactivateUserAsync(userId);
                return Ok(new { message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // =====================================================
        // ✅ FILTER USERS BY ROLE & ACTIVE STATUS
        // =====================================================
        [HttpGet("filter")]
        public async Task<IActionResult> Filter(
            [FromQuery] string? role,
            [FromQuery] bool? isActive)
        {
            var users = await _userService.FilterUsersAsync(role, isActive);
            return Ok(users);
        }
    }
}
