using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services.Interfaces;

namespace CRM.Server.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditRepository _auditRepository;

        public AuditLogService(IAuditRepository auditRepository)
        {
            _auditRepository = auditRepository;
        }

        public async Task LogAsync(string userId, string action, string? oldValue = null, string? newValue = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                OldValue = oldValue,
                NewValue = newValue
            };

            await _auditRepository.AddAsync(log);
        }
    }
}
