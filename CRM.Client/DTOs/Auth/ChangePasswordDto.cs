using System.ComponentModel.DataAnnotations;

namespace CRM.Client.DTOs.Auth
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{6,}$",
            ErrorMessage = "Password must contain uppercase, lowercase, number, and special character."
        )]
        public string NewPassword { get; set; } = string.Empty;
    }
}
