using System.ComponentModel.DataAnnotations;

namespace CRM.Server.DTOs.Users
{
    public class UpdateUserDto
    {
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 50 characters.")]
        [RegularExpression("^[A-Za-z ]+$", ErrorMessage = "Full name can contain only letters and spaces.")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, MinimumLength = 3, ErrorMessage = "Role must be between 3 and 20 characters.")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "Role can contain only letters, numbers, and underscores.")]
        public string Role { get; set; } = string.Empty;

        public bool? IsActive { get; set; }  // Optional
    }
}
