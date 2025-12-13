using CRM.Server.DTOs.Roles;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        // =============================================
        // CREATE ROLE
        // =============================================
        [HttpPost("create")]
        public async Task<IActionResult> CreateRole(CreateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.RoleName))
                return BadRequest("RoleName is required.");

            // ⭐ ADDED: Normalize the input
            var normalizedName = dto.RoleName.Trim().ToUpper();

            // ⭐ ADDED: Check if role already exists (case-insensitive)
            var existing = await _roleService.GetRoleAsync(normalizedName);
            if (existing != null)
            {
                return Conflict("Role already exists.");
            }

            try
            {
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await _roleService.CreateRoleAsync(normalizedName, performedBy!);

                return Ok("Role created successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =============================================
        // ASSIGN ROLE
        // =============================================
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

        // =============================================
        // GET ALL ROLES
        // =============================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles);
        }

        // =============================================
        // GET ROLE BY NAME
        // =============================================
        [HttpGet("{name}")]
        public async Task<IActionResult> Get(string name)
        {
            var role = await _roleService.GetRoleAsync(name);
            if (role == null) return NotFound("Role not found");
            return Ok(role);
        }

        // =============================================
        // UPDATE ROLE
        // =============================================
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OldName) ||
                string.IsNullOrWhiteSpace(dto.NewName))
            {
                return BadRequest("OldName and NewName are required.");
            }

            // ⭐ ADDED: Normalize names
            var oldNameNormalized = dto.OldName.Trim().ToUpper();
            var newNameNormalized = dto.NewName.Trim().ToUpper();

            // ⭐ ADDED: Prevent NewName == OldName
            if (oldNameNormalized == newNameNormalized)
            {
                return BadRequest("New role name cannot be the same as the old role name.");
            }

            // ⭐ ADDED: Prevent renaming into an existing role
            var existing = await _roleService.GetRoleAsync(newNameNormalized);
            if (existing != null)
            {
                return Conflict("A role with this name already exists.");
            }

            try
            {
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await _roleService.UpdateRoleAsync(oldNameNormalized, newNameNormalized, performedBy!);

                return Ok("Role updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =============================================
        // DELETE ROLE
        // =============================================
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _roleService.DeleteRoleAsync(name, performedBy!);

            return Ok("Role deleted successfully");
        }
    }
}
