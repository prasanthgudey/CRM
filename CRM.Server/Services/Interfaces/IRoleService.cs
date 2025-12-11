using Microsoft.AspNetCore.Identity;
using CRM.Server.Models;

namespace CRM.Server.Services.Interfaces
{
    public interface IRoleService
    {
        Task CreateRoleAsync(string roleName);
        Task<List<IdentityRole>> GetAllRolesAsync();
        Task<IdentityRole?> GetRoleAsync(string roleName);
        Task UpdateRoleAsync(string oldName, string newName);
        Task DeleteRoleAsync(string roleName);
        Task AssignRoleAsync(string userId, string roleName);
    }

}
