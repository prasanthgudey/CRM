using System.ComponentModel.DataAnnotations;

namespace CRM.Server.DTOs.Auth
{
    public class RefreshRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
