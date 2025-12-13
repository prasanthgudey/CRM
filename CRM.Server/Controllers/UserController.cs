using CRM.Server.DTOs.Roles;
using CRM.Server.DTOs.Users;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // =============================================
        // CREATE USER
        // =============================================
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            _logger.LogInformation("CreateUser called");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.CreateUserAsync(dto, performedBy!);

            return Ok(new
            {
                success = true,
                message = "User created successfully"
            });
        }

        // =============================================
        // GET ALL USERS
        // =============================================
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            _logger.LogInformation("Fetching all users");
            return Ok(await _userService.GetAllUsersAsync());
        }

        // =============================================
        // INVITE USER
        // =============================================
        [HttpPost("invite")]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserDto dto)
        {
            _logger.LogInformation("InviteUser called");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.InviteUserAsync(dto, performedBy!);

            return Ok(new
            {
                success = true,
                message = "Invitation sent successfully"
            });
        }

        // =============================================
        // DEACTIVATE USER
        // =============================================
        [HttpPut("deactivate/{userId}")]
        public async Task<IActionResult> Deactivate(string userId)
        {
            _logger.LogInformation($"DeactivateUser called for: {userId}");

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.DeactivateUserAsync(userId, performedBy!);

            return Ok(new { message = "User deactivated successfully" });
        }

        // =============================================
        // ACTIVATE USER
        // =============================================
        [HttpPut("activate/{userId}")]
        public async Task<IActionResult> Activate(string userId)
        {
            _logger.LogInformation($"ActivateUser called for: {userId}");

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.ActivateUserAsync(userId, performedBy!);

            return Ok(new { message = "User activated successfully" });
        }

        // =============================================
        // UPDATE USER
        // =============================================
        [HttpPut("update/{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserDto dto)
        {
            _logger.LogInformation($"UpdateUser called for: {userId}");

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.UpdateUserAsync(userId, dto, performedBy!);

            return Ok(new { message = "User updated successfully" });
        }

        // =============================================
        // DELETE USER
        // =============================================
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            _logger.LogInformation($"DeleteUser called for: {userId}");

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.DeleteUserAsync(userId, performedBy!);

            return Ok(new { message = "User deleted successfully" });
        }

        // =============================================
        // GET USER BY ID  (NO TRY/CATCH)
        // =============================================
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            _logger.LogInformation($"GetUserById called for: {userId}");

            var user = await _userService.GetUserByIdAsync(userId);
            return Ok(user);
        }

        // =============================================
        // GET LOGGED-IN USER PROFILE
        // =============================================
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            _logger.LogInformation("My profile requested");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userService.GetUserByIdAsync(userId!);

            return Ok(user);
        }

        // =============================================
        // ASSIGN ROLE
        // =============================================
        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole(AssignRoleDto dto)
        {
            _logger.LogInformation($"AssignRole called for user {dto.UserId}");

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _userService.AssignRoleAsync(dto.UserId, dto.RoleName, performedBy!);

            return Ok("Role assigned successfully");
        }

        // =============================================
        // FILTER USERS
        // =============================================
        [HttpGet("filter")]
        public async Task<IActionResult> Filter([FromQuery] string? role, [FromQuery] bool? isActive)
        {
            _logger.LogInformation("FilterUsers called");
            return Ok(await _userService.FilterUsersAsync(role, isActive));
        }
    }
}
