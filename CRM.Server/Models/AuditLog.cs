namespace CRM.Server.Models
{
    // ✅ Fully custom entity used by EF Core
    public class AuditLog
    {
        public int Id { get; set; }

        // ✅ Who performed the action (may be null for system events)
        public string? PerformedByUserId { get; set; }

        // ✅ Who the action was performed on (if applicable)
        public string? TargetUserId { get; set; }

        // ✅ Action name: Login, Logout, Create, Update, Delete, RoleChanged, RoleAssigned, etc.
        public string Action { get; set; } = string.Empty;

        // ✅ Target entity: User, Role, Task, Customer, Appointment, etc.
        public string EntityName { get; set; } = string.Empty;

        // ✅ Old value (JSON/text) before change
        public string? OldValue { get; set; }

        // ✅ New value (JSON/text) after change
        public string? NewValue { get; set; }

        // ✅ IP address for security events (login/logout)
        public string? IpAddress { get; set; }

        // ✅ Whether the operation succeeded (true/false)
        public bool IsSuccess { get; set; }

        // ✅ Timestamp (UTC)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
