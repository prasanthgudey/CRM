namespace CRM.Server.Services.Interfaces
{
    public interface IRoleService
    {
        Task CreateRoleAsync(string roleName);
        Task AssignRoleAsync(string userId, string roleName);
    }
}
