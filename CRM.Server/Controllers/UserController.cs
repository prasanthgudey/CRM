using CRM.Server.DTOs.Users;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin")] // ✅ Admin only
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
        //[HttpPost("create")]
        //public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    try
        //    {
        //        await _userService.CreateUserAsync(dto);
        //        return Ok(new { message = "User created successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { error = ex.Message });
        //  
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _userService.CreateUserAsync(dto);

                return Ok(new
                {
                    success = true,                 // ⭐ NEW
                    message = "User created successfully" // ⭐ NEW
                });
            }
            catch (Exception ex)
            {
                return Ok(new                      // ⭐ CHANGED FROM BadRequest to Ok
                {                                  //   so frontend always receives JSON safely
                    success = false,               // ⭐ NEW
                    message = ex.Message           // ⭐ NEW (eg: "Email already exists")
                });
            }
        }

        //}

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
        //[HttpPost("invite")]
        //public async Task<IActionResult> InviteUser([FromBody] InviteUserDto dto)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    try
        //    {
        //        await _userService.InviteUserAsync(dto);
        //        return Ok(new { message = "Invitation sent successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { error = ex.Message });
        //    }
        //}

        [HttpPost("invite")]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _userService.InviteUserAsync(dto);

                // ✅ ALWAYS JSON, includes success flag
                return Ok(new
                {
                    success = true,
                    message = "Invitation sent successfully"
                });
            }
            catch (Exception ex)
            {
                // ✅ ALSO JSON on error (eg: "Email already exists")
                return Ok(new
                {
                    success = false,
                    message = ex.Message
                });
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


        // ✅ ACTIVATE USER
        // =====================================================
        [HttpPut("activate/{userId}")]
        public async Task<IActionResult> Activate(string userId)
        {
            try
            {
                await _userService.ActivateUserAsync(userId);
                return Ok(new { message = "User activated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // =====================================================
        // ✅ UPDATE USER
        // =====================================================
        [HttpPut("update/{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _userService.UpdateUserAsync(userId, dto);
                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }




        // =====================================================
        // ✅ DELETE USER
        // =====================================================
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                await _userService.DeleteUserAsync(userId);
                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        // =====================================================
        // ✅ GET USER BY ID (ADMIN / PROFILE VIEW)
        // =====================================================
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
