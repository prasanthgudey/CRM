using CRM.Server.Models;
using CRM.Server.Repositories;
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

        public async Task LogAsync(
            string? performedByUserId,
            string? targetUserId,
            string action,
            string entityName,
            bool isSuccess,
            string? ipAddress = null,
            string? oldValue = null,
            string? newValue = null)
        {
            var log = new AuditLog
            {
                PerformedByUserId = performedByUserId,
                TargetUserId = targetUserId,
                Action = action,
                EntityName = entityName,
                IsSuccess = isSuccess,
                IpAddress = ipAddress,
                OldValue = oldValue,
                NewValue = newValue,
                CreatedAt = DateTime.UtcNow
            };

            await _auditRepository.AddAsync(log);
        }
        public async Task<int> GetTotalCountAsync()
        {
            var all = await _auditRepository.GetAllAsync();
            return all?.Count ?? 0;
        }
    }
}
// NEW: Number of failed (IsSuccess = false) logs in last X days
