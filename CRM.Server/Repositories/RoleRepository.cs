using CRM.Server.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.Server.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleRepository(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<List<IdentityRole>> GetAllAsync()
        {
            return await _roleManager.Roles.ToListAsync();
        }

        public async Task<IdentityRole?> GetByNameAsync(string roleName)
        {
            return await _roleManager.FindByNameAsync(roleName);
        }

        public async Task CreateAsync(string roleName)
        {
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        public async Task UpdateAsync(IdentityRole role)
        {
            await _roleManager.UpdateAsync(role);
        }

        public async Task DeleteAsync(IdentityRole role)
        {
            await _roleManager.DeleteAsync(role);
        }
    }
}
