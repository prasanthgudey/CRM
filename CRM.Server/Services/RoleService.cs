using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using CRM.Server.Models;
//using Microsoft.AspNetCore.Identity;

namespace CRM.Server.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleService(IRoleRepository roleRepository, UserManager<ApplicationUser> userManager)
        {
            _roleRepository = roleRepository;
            _userManager = userManager;
        }

        public async Task CreateRoleAsync(string roleName)
        {
            await _roleRepository.CreateAsync(roleName);
        }

        public async Task<List<IdentityRole>> GetAllRolesAsync()
        {
            return await _roleRepository.GetAllAsync();
        }

        public async Task<IdentityRole?> GetRoleAsync(string roleName)
        {
            return await _roleRepository.GetByNameAsync(roleName);
        }

        public async Task UpdateRoleAsync(string oldName, string newName)
        {
            var role = await _roleRepository.GetByNameAsync(oldName);

            if (role == null)
                throw new Exception("Role not found.");

            role.Name = newName;
            role.NormalizedName = newName.ToUpper();

            await _roleRepository.UpdateAsync(role);
        }

        public async Task DeleteRoleAsync(string roleName)
        {
            var role = await _roleRepository.GetByNameAsync(roleName);

            if (role == null)
                throw new Exception("Role not found.");

            await _roleRepository.DeleteAsync(role);
        }

        public async Task AssignRoleAsync(string userId, string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new Exception("Role name cannot be empty.");

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                throw new Exception("User not found");

            // Check role existence
            var role = await _roleRepository.GetByNameAsync(roleName);
            if (role == null)
                throw new Exception("Role does not exist.");

            // Get current roles
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Case 1: User already has the role → do nothing
            if (currentRoles.Contains(roleName))
                return;

            // Case 2: User has no roles → directly add
            if (!currentRoles.Any())
            {
                var add = await _userManager.AddToRoleAsync(user, roleName);
                if (!add.Succeeded)
                    throw new Exception("Failed to assign role.");
                return;
            }

            // Case 3: User has different roles → replace all in one safe flow
            // Remove old roles
            var remove = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!remove.Succeeded)
                throw new Exception("Failed to remove existing roles.");

            // Add new role
            var addResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!addResult.Succeeded)
                throw new Exception("Failed to assign new role.");
        }


    }
}
