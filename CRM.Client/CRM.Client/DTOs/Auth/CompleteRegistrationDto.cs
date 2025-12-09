// Backend reference:
// public async Task<IActionResult> CompleteRegistration(CompleteRegistrationDto dto)
// Uses:
// dto.Email
// dto.Token
// dto.Password

namespace CRM.Client.DTOs.Auth
{
    // ✅ USER-DEFINED: Invite registration completion model
    public class CompleteRegistrationDto
    {
        public string Email { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
