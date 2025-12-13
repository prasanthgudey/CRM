using System.ComponentModel.DataAnnotations;

namespace CRM.Client.DTOs.Users
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Name is required")]

        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$",
        ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        // ✅ Admin assigns role at creation
        [Required(ErrorMessage = "Role is required")]

        public string Role { get; set; } = string.Empty;
    }
}
