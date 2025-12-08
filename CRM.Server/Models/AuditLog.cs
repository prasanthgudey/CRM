namespace CRM.Server.Models
{
    // ✅ Fully custom entity
    public class AuditLog
    {
        public int Id { get; set; }

        // ✅ Which user performed the action
        public string? UserId { get; set; }

        // ✅ What happened (Login, Logout, RoleAssigned, RoleRemoved, etc.)
        public string Action { get; set; } = string.Empty;

        // ✅ Optional: Which entity was affected (User, Role, etc.)
        public string? EntityName { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        // ✅ For security tracking
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
