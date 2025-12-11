using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Server.Models
{
    public class RefreshToken
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string Token { get; set; } = null!;              // opaque secure token string

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }                 // expiration datetime
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; }

        // For rotation: link to the token that replaced this one (optional)
        public string? ReplacedByToken { get; set; }

        // Optional auditing / device info
        public string? CreatedByIp { get; set; }
        public string? UserAgent { get; set; }

        // FK
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}
