using CRM.Server.Models;

namespace CRM.Server.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByIdAsync(string userId);
        Task<ApplicationUser?> GetByEmailAsync(string email);
        Task<List<ApplicationUser>> GetAllAsync();
        Task AddAsync(ApplicationUser user);
        Task UpdateAsync(ApplicationUser user);
        Task DeleteAsync(ApplicationUser user);
        //Task<List<ApplicationUser>> FilterAsync(string? role, bool? isActive);

    }
}
