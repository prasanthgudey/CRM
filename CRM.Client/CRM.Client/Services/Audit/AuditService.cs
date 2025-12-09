using CRM.Client.DTOs.Audit;
using CRM.Client.Services.Http;

namespace CRM.Client.Services.Audit
{
    // ✅ USER-DEFINED: Read-only audit service
    public class AuditService
    {
        private readonly ApiClientService _api;

        public AuditService(ApiClientService api)
        {
            _api = api;
        }

        // ✅ GET ALL AUDIT LOGS
        public async Task<List<AuditLogResponseDto>?> GetAllLogsAsync()
        {
            return await _api.GetAsync<List<AuditLogResponseDto>>("api/audit");
        }
    }
}
