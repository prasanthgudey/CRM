namespace CRM.Server.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(
            string? performedByUserId,
            string? targetUserId,
            string action,
            string entityName,
            bool isSuccess,
            string? ipAddress = null,
            string? oldValue = null,
            string? newValue = null);
        // NEW: Dashboard total audit logs count
        Task<int> GetTotalCountAsync();

    }


}
