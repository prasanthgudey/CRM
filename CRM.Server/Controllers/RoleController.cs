using CRM.Server.DTOs.Roles;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin")] // ✅ Admin only
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        // ✅ Custom service
        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRole(CreateRoleDto dto)
        {
            await _roleService.CreateRoleAsync(dto.RoleName);
            return Ok("Role created successfully");
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole(AssignRoleDto dto)
        {
            try
            {
                await _roleService.AssignRoleAsync(dto.UserId, dto.RoleName);
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

        [HttpPut("update")]
        public async Task<IActionResult> UpdateRole(UpdateRoleDto dto)
        {
            await _roleService.UpdateRoleAsync(dto.OldName, dto.NewName);
            return Ok("Role updated successfully");
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            await _roleService.DeleteRoleAsync(name);
            return Ok("Role deleted successfully");
        }

    }
}
