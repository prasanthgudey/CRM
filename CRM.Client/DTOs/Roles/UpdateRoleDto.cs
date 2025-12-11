//using System.ComponentModel.DataAnnotations;

//public class UpdateRoleDto
//{
//    // *** FIX: make sure Id exists and is sent ***
//    public Guid Id { get; set; }   // <-- ADD THIS PROPERTY

//    [Required]
//    public string RoleName { get; set; } = string.Empty;
//}
using System.ComponentModel.DataAnnotations;

namespace CRM.Client.DTOs.Roles
{
    public class UpdateRoleDto
    {
        // Name of the role before editing (from the list)
        public string OldName { get; set; } = string.Empty;

        // New name typed in the Edit modal
        [Required]
        public string NewName { get; set; } = string.Empty;
    }
}
