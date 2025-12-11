using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using CRM.Server.Models;
using System.Text.Json;

namespace CRM.Server.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService; // ✅ ADDED

        public RoleService(
            IRoleRepository roleRepository,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService) // ✅ ADDED
        {
            _roleRepository = roleRepository;
            _userManager = userManager;
            _auditLogService = auditLogService;
        }

        public async Task CreateRoleAsync(string roleName, string performedByUserId)
        {
            await _roleRepository.CreateAsync(roleName);

            await SafeAudit(
                performedByUserId,
                null,
                "Role Created",
                "Role",
                true,
                null,
                null,
                JsonSerializer.Serialize(new { RoleName = roleName })
            );
        }

        public async Task<List<IdentityRole>> GetAllRolesAsync()
        {
            return await _roleRepository.GetAllAsync();
        }

        public async Task<IdentityRole?> GetRoleAsync(string roleName)
        {
            return await _roleRepository.GetByNameAsync(roleName);
        }

        public async Task UpdateRoleAsync(string oldName, string newName, string performedByUserId)
        {
            var role = await _roleRepository.GetByNameAsync(oldName);

            if (role == null)
                throw new Exception("Role not found.");

            var oldValue = JsonSerializer.Serialize(new { role.Name });

            role.Name = newName;
            role.NormalizedName = newName.ToUpper();

            await _roleRepository.UpdateAsync(role);

            var newValue = JsonSerializer.Serialize(new { role.Name });

            await SafeAudit(
                performedByUserId,
                null,
                "Role Updated",
                "Role",
                true,
                null,
                oldValue,
                newValue
            );
        }

        public async Task DeleteRoleAsync(string roleName, string performedByUserId)
        {
            var role = await _roleRepository.GetByNameAsync(roleName);

            if (role == null)
                throw new Exception("Role not found.");

            var oldValue = JsonSerializer.Serialize(new { role.Name });

            await _roleRepository.DeleteAsync(role);

            await SafeAudit(
                performedByUserId,
                null,
                "Role Deleted",
                "Role",
                true,
                null,
                oldValue,
                null
            );
        }

        public async Task AssignRoleAsync(string userId, string roleName, string performedByUserId)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new Exception("Role name cannot be empty.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var role = await _roleRepository.GetByNameAsync(roleName);
            if (role == null)
                throw new Exception("Role does not exist.");

            var currentRoles = await _userManager.GetRolesAsync(user);

            //var oldValue = JsonSerializer.Serialize(currentRoles);
             var oldValue = currentRoles.FirstOrDefault() ?? "";



            if (currentRoles.Contains(roleName))
                return;

            if (currentRoles.Any())
            {
                var remove = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!remove.Succeeded)
                    throw new Exception("Failed to remove existing roles.");
            }

            var addResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!addResult.Succeeded)
                throw new Exception("Failed to assign new role.");

            //var newValue = JsonSerializer.Serialize(new { AssignedRole = roleName });
            var newValue = roleName;
            await SafeAudit(
                performedByUserId,
                userId, // ✅ TARGET USER
                "Role Assigned",
                "UserRole",
                true,
                null,
                oldValue,
                newValue
            );
        }

        // ===========================================
        // ✅ SAFE AUDIT WRAPPER
        // ===========================================
        private async Task SafeAudit(
            string? performedByUserId,
            string? targetUserId,
            string action,
            string entityName,
            bool isSuccess,
            string? ipAddress,
            string? oldValue,
            string? newValue)
        {
            try
            {
                await _auditLogService.LogAsync(
                    performedByUserId,
                    targetUserId,
                    action,
                    entityName,
                    isSuccess,
                    ipAddress,
                    oldValue,
                    newValue
                );
            }
            catch
            {
                // ✅ Never break business flow due to audit failure
            }
        }
    }
}
