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
            if (user != null)
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }
        }
    }
}
