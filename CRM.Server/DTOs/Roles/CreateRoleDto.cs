using System.ComponentModel.DataAnnotations;

namespace CRM.Server.DTOs.Roles
{
    public class CreateRoleDto
    {
        [Required(ErrorMessage = "RoleName is required.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "RoleName must be between 3 and 20 characters.")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "RoleName can contain only letters, numbers, and underscores.")]
        public string RoleName { get; set; } = string.Empty;
    }
}
