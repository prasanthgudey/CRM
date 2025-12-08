using Microsoft.AspNetCore.Identity;

namespace CRM.Server.Repositories.Interfaces
{
    public interface IRoleRepository
    {
        Task<List<IdentityRole>> GetAllAsync();
        Task<IdentityRole?> GetByNameAsync(string roleName);
        Task CreateAsync(string roleName);
    }
}
