using CRM.Server.DTOs.Roles;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // ✅ Enforced as per spec
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        //[HttpPost("create")]
        //public async Task<IActionResult> CreateRole(CreateRoleDto dto)
        //{
        //    await _roleService.CreateRoleAsync(dto.RoleName);
        //    return Ok("Role created successfully");
        //}
        [HttpPost("create")]
        public async Task<IActionResult> CreateRole(CreateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.RoleName))
                return BadRequest("RoleName is required.");

            // CHECK: if role already exists -> return 409
            var existing = await _roleService.GetRoleAsync(dto.RoleName);
            if (existing != null)
            {
                return Conflict("Role already exists");
            }

            try
            {
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await _roleService.CreateRoleAsync(dto.RoleName, performedBy!);

                return Ok("Role created successfully");
            }
            catch (Exception ex)
            {
                // keep the same behavior: return BadRequest with message on other errors
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole(AssignRoleDto dto)
        {
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                await _roleService.AssignRoleAsync(dto.UserId, dto.RoleName, performedBy!);
                return Ok("Role assigned successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> Get(string name)
        {
            var role = await _roleService.GetRoleAsync(name);
            if (role == null) return NotFound("Role not found");
            return Ok(role);
        }

        //[HttpPut("update")]
        //public async Task<IActionResult> UpdateRole(UpdateRoleDto dto)
        //{
        //    await _roleService.UpdateRoleAsync(dto.OldName, dto.NewName);
        //    return Ok("Role updated successfully");
        //}
        // ===============================
        // UPDATE ROLE
        // ===============================
        //[HttpPut("update")]
        //public async Task<IActionResult> Update([FromBody] UpdateRoleDto dto)
        //{
        //    if (string.IsNullOrWhiteSpace(dto.OldName) ||
        //        string.IsNullOrWhiteSpace(dto.NewName))
        //    {
        //        return BadRequest("OldName and NewName are required.");
        //    }

        //    await _roleService.UpdateRoleAsync(dto.OldName, dto.NewName);

        //    // No body, just status 204 = success
        //    return NoContent();
        //}
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OldName) ||
                string.IsNullOrWhiteSpace(dto.NewName))
            {
                return BadRequest("OldName and NewName are required.");
            }

            // If the new name equals the old name (case-insensitive), treat as no-op
            if (string.Equals(dto.OldName, dto.NewName, StringComparison.OrdinalIgnoreCase))
            {
                // nothing to change — return 204 as before
                return NoContent();
            }

            // CHECK: if new name already exists -> return 409
            var existing = await _roleService.GetRoleAsync(dto.NewName);
            if (existing != null)
            {
                return Conflict("Role already exists");
            }

            try
            {
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await _roleService.UpdateRoleAsync(dto.OldName, dto.NewName, performedBy!);

                return Ok("Role updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _roleService.DeleteRoleAsync(name, performedBy!);

            return Ok("Role deleted successfully");
        }
    }
}
