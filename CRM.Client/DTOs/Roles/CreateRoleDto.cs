using System.ComponentModel.DataAnnotations;

namespace CRM.Client.DTOs.Roles
{
    public class CreateRoleDto
    {
        [Required(ErrorMessage = "Role name is required")]
        public string RoleName { get; set; } = string.Empty;
    }
}
