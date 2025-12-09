using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using CRM.Server.Models;

namespace CRM.Server.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleService(
            IRoleRepository roleRepository,
            UserManager<ApplicationUser> userManager)
        {
            _roleRepository = roleRepository;
            _userManager = userManager;
        }

        public async Task CreateRoleAsync(string roleName)
        {
            await _roleRepository.CreateAsync(roleName);
        }

        public async Task AssignRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                throw new Exception("User not found");

            // ✅ 1. Get all current roles
            var currentRoles = await _userManager.GetRolesAsync(user);

            // ✅ 2. Remove all existing roles
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!removeResult.Succeeded)
                    throw new Exception("Failed to remove existing roles");
            }

            // ✅ 3. Add new role
            var addResult = await _userManager.AddToRoleAsync(user, roleName);

            if (!addResult.Succeeded)
                throw new Exception("Failed to assign new role");
        }

    }
}
