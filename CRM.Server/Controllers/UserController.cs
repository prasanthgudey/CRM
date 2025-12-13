using CRM.Server.DTOs.Auth;
using CRM.Server.DTOs.Roles;
using CRM.Server.DTOs.Users;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.Server.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ⭐ DUPLICATE CHECK ADDED
            var users = await _userService.GetAllUsersAsync();
            var normalizedEmail = dto.Email?.Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(normalizedEmail) &&
                users.Any(u => u.Email != null &&
                               u.Email.Trim().ToLower() == normalizedEmail))
            {
                return Ok(new
                {
                    success = false,
                    message = "A user with this email already exists."
                });
            }

            try
            {
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _userService.CreateUserAsync(dto, performedBy!);

                return Ok(new
                {
                    success = true,                 // ⭐ NEW
                    message = "User created successfully" // ⭐ NEW
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost("invite")]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ⭐ DUPLICATE CHECK ADDED
            var users = await _userService.GetAllUsersAsync();
            var normalizedEmail = dto.Email?.Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(normalizedEmail) &&
                users.Any(u => u.Email != null &&
                               u.Email.Trim().ToLower() == normalizedEmail))
            {
                return Ok(new
                {
                    success = false,
                    message = "A user with this email already exists."
                });
            }

            try
            {
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _userService.InviteUserAsync(dto, performedBy!);

                return Ok(new
                {
                    success = true,
                    message = "Invitation sent successfully"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPut("deactivate/{userId}")]
        public async Task<IActionResult> Deactivate(string userId)
        {
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.DeactivateUserAsync(userId, performedBy!);
            return Ok(new { message = "User deactivated successfully" });
        }

        [HttpPut("activate/{userId}")]
        public async Task<IActionResult> Activate(string userId)
        {
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.ActivateUserAsync(userId, performedBy!);
            return Ok(new { message = "User activated successfully" });
        }

        [HttpPut("update/{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserDto dto)
        {
            // ⭐ DUPLICATE CHECK ADDED
            var users = await _userService.GetAllUsersAsync();

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var normalizedEmail = dto.Email.Trim().ToLower();

                if (users.Any(u =>
                        u.Id != userId &&
                        u.Email != null &&
                        u.Email.Trim().ToLower() == normalizedEmail))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Another user already uses this email."
                    });
                }
            }

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.UpdateUserAsync(userId, dto, performedBy!);
            return Ok(new { message = "User updated successfully" });
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.DeleteUserAsync(userId, performedBy!);
            return Ok(new { message = "User deleted successfully" });
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // =====================================================
        // ✅ GET MY PROFILE (LOGGED-IN USER)
        // =====================================================
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            var user = await _userService.GetUserByIdAsync(userId!);

            return Ok(user);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole(AssignRoleDto dto)
        {
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                await _userService.AssignRoleAsync(dto.UserId, dto.RoleName, performedBy!);
                return Ok("Role assigned successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("filter")]
        public async Task<IActionResult> Filter([FromQuery] string? role, [FromQuery] bool? isActive)
        {
            return Ok(await _userService.FilterUsersAsync(role, isActive));
        }
    }
}
