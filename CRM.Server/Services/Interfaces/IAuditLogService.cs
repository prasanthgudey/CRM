namespace CRM.Server.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(string userId, string action, string? oldValue = null, string? newValue = null);
    }
}
