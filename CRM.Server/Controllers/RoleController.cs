using CRM.Server.DTOs.Roles;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // ✅ Admin only
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
            await _roleService.AssignRoleAsync(dto.UserId, dto.RoleName);
            return Ok("Role assigned successfully");
        }
    }
}
