using CRM.Server.Data;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRM.Server.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly ApplicationDbContext _context;

        public AuditRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AuditLog log)
        {
            await _context.AuditLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetAllAsync()
        {
            return await _context.AuditLogs
                                 .OrderByDescending(x => x.CreatedAt)
                                 .ToListAsync();
        }
    }
}
