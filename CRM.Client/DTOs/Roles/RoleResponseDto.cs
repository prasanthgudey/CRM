using System.ComponentModel.DataAnnotations;
namespace CRM.Client.DTOs.Roles
{
    public class RoleResponseDto
    {
        public string Id { get; set; } = string.Empty;
        [Required]
        public string Name { get; set; } = string.Empty;
    }

}
