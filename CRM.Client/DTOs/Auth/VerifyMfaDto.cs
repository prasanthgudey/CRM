using System.ComponentModel.DataAnnotations;

namespace CRM.Client.DTOs.Auth
{
    public class VerifyMfaDto
    {
        [Required]
        [MinLength(6), MaxLength(6)]
        public string Code { get; set; } = string.Empty;
    }
}
