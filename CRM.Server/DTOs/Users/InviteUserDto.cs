using System.ComponentModel.DataAnnotations;

namespace CRM.Server.DTOs.Users
{
    public class InviteUserDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 50 characters.")]
        [RegularExpression("^[A-Za-z ]+$", ErrorMessage = "Full name can contain only letters and spaces.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Role must be between 3 and 20 characters.")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "Role can contain only letters, numbers, and underscores.")]
        public string Role { get; set; } = string.Empty;
    }
}
