using CRM.Client.DTOs.Audit;
using CRM.Client.Services.Http;

namespace CRM.Client.Services.Audit
{
    public class AuditService
    {
        private readonly ApiClientService _api;

        public AuditService(ApiClientService api)
        {
            _api = api;
        }

        // ✅ GET ALL AUDIT LOGS
        public async Task<List<AuditLogResponseDto>?> GetAllAsync()
        {
            return await _api.GetAsync<List<AuditLogResponseDto>>("api/audit");
        }
        // NEW: client-side helper to fetch total audit count
        public async Task<int> GetTotalCountAsync()
        {
            var result = await _api.GetAsync<int?>("api/audit/count");
            return result ?? 0;
        }

    }
}
