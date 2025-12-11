namespace CRM.Client.DTOs.Audit
{
    public class AuditLogResponseDto
    {
        public int Id { get; set; }

        public string? PerformedByUserId { get; set; }
        public string? PerformedByUserName { get; set; }

        public string? TargetUserId { get; set; }
        public string? TargetUserName { get; set; }

        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public string? IpAddress { get; set; }

        public bool IsSuccess { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
