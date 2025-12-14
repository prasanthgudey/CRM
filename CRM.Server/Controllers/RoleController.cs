using CRM.Server.DTOs.Roles;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<RoleController> _logger;

        public RoleController(IRoleService roleService, ILogger<RoleController> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        // ================================
        // CREATE ROLE
        // ================================
        [HttpPost("create")]
        public async Task<IActionResult> CreateRole(CreateRoleDto dto)
        {
            _logger.LogInformation("CreateRole called with: {RoleName}", dto?.RoleName);

            if (string.IsNullOrWhiteSpace(dto?.RoleName))
            {
                _logger.LogWarning("CreateRole failed — RoleName missing");
                return BadRequest("RoleName is required.");
            }

            // ⭐ ADDED: Normalize the input
            var normalizedName = dto.RoleName.Trim().ToUpper();

            // ⭐ ADDED: Check if role already exists (case-insensitive)
            var existing = await _roleService.GetRoleAsync(normalizedName);
            if (existing != null)
            {
                _logger.LogWarning("CreateRole conflict — Role already exists: {RoleName}", dto.RoleName);
                return Conflict("Role already exists");
            }

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ❗ No try/catch → exceptions go to GlobalExceptionMiddleware
            await _roleService.CreateRoleAsync(dto.RoleName, performedBy!);

            _logger.LogInformation("Role created successfully: {RoleName}", dto.RoleName);

            return Ok("Role created successfully");
        }

        // ================================
        // ASSIGN ROLE
        // ================================
        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole(AssignRoleDto dto)
        {
            _logger.LogInformation("AssignRole called for UserId={UserId}, Role={Role}",
                dto.UserId, dto.RoleName);

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _roleService.AssignRoleAsync(dto.UserId, dto.RoleName, performedBy!);

            _logger.LogInformation("Role assigned successfully: {Role}", dto.RoleName);

            return Ok("Role assigned successfully");
        }

        // ================================
        // GET ALL ROLES
        // ================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Fetching all roles");

            var roles = await _roleService.GetAllRolesAsync();

            return Ok(roles);
        }

        // ================================
        // GET ROLE BY NAME
        // ================================
        [HttpGet("{name}")]
        public async Task<IActionResult> Get(string name)
        {
            _logger.LogInformation("Fetching role by name: {RoleName}", name);

            var role = await _roleService.GetRoleAsync(name);

            if (role == null)
            {
                _logger.LogWarning("Role not found: {RoleName}", name);
                return NotFound("Role not found");
            }

            return Ok(role);
        }

        // ================================
        // UPDATE ROLE
        // ================================
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateRoleDto dto)
        {
            _logger.LogInformation("Update role request OldName={OldName}, NewName={NewName}",
                dto.OldName, dto.NewName);

            if (string.IsNullOrWhiteSpace(dto.OldName) ||
                string.IsNullOrWhiteSpace(dto.NewName))
            {
                _logger.LogWarning("UpdateRole failed — missing names");
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

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await _roleService.UpdateRoleAsync(oldNameNormalized, newNameNormalized, performedBy!);

            _logger.LogInformation("Role updated successfully: {Old} -> {New}",
                dto.OldName, dto.NewName);

            return Ok("Role updated successfully");
        }

        // ================================
        // DELETE ROLE
        // ================================
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            _logger.LogInformation("Delete role request: {RoleName}", name);

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _roleService.DeleteRoleAsync(name, performedBy!);

            _logger.LogInformation("Role deleted successfully: {RoleName}", name);

            return Ok("Role deleted successfully");
        }
    }
}
