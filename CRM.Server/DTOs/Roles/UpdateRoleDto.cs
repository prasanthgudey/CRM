using System.ComponentModel.DataAnnotations;

namespace CRM.Server.DTOs.Roles
{
    public class UpdateRoleDto
    {
        [Required(ErrorMessage = "OldName is required.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "OldName must be between 3 and 20 characters.")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "OldName can contain only letters, numbers, and underscores.")]
        public string OldName { get; set; }

        [Required(ErrorMessage = "NewName is required.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "NewName must be between 3 and 20 characters.")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "NewName can contain only letters, numbers, and underscores.")]
        public string NewName { get; set; }
    }
}
