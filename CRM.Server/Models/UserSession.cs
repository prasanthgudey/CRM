using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Server.Models
{
    public class UserSession
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = null!;

        // Optional link to a refresh token row (if you choose to link)
        public string? RefreshTokenId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Updated on each authenticated request
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        // Optional absolute expiry (can be null if you don't use absolute lifetime)
        public DateTime? ExpiresAt { get; set; }

        public bool IsRevoked { get; set; } = false;

        // Optional auditing fields
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        // FK relation for convenience (optional)
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}
