using Microsoft.AspNetCore.Identity;
using CRM.Server.Models;

namespace CRM.Server.Services.Interfaces
{
    public interface IRoleService
    {
        // ✅ Role Management with Audit Support
        Task CreateRoleAsync(string roleName, string performedByUserId);

        Task<List<IdentityRole>> GetAllRolesAsync();

        Task<IdentityRole?> GetRoleAsync(string roleName);

        Task UpdateRoleAsync(string oldName, string newName, string performedByUserId);

        Task DeleteRoleAsync(string roleName, string performedByUserId);

        Task AssignRoleAsync(string userId, string roleName, string performedByUserId);
    }
}
