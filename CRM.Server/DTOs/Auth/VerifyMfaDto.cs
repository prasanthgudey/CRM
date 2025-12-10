using System.ComponentModel.DataAnnotations;

namespace CRM.Server.DTOs.Auth
{
    public class VerifyMfaDto
    {
        [Required]
        public string Code { get; set; } = string.Empty; // 6-digit OTP
    }
}
