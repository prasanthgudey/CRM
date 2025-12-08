using CRM.Server.Models;

namespace CRM.Server.Repositories.Interfaces
{
    public interface IAuditRepository
    {
        Task AddAsync(AuditLog log);
        Task<List<AuditLog>> GetAllAsync();
    }
}
