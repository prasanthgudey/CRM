using Microsoft.AspNetCore.Identity;

namespace CRM.Server.Models
{
    // ✅ Built-in framework base class: IdentityUser
    // ✅ Custom user entity for your application
    public class ApplicationUser : IdentityUser
    {
        // ✅ Custom fields (you can extend more later)

        public string? FullName { get; set; }

        public bool IsActive { get; set; } = true;

        // ✅ Used for password expiry & security tracking
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsInvitePending { get; set; } = false;
        public DateTime? InviteExpiry { get; set; }

    }
}
