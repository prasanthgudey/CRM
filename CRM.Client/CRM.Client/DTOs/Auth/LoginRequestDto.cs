// Backend reference:
// public async Task<IActionResult> Login(LoginRequestDto dto)

namespace CRM.Client.DTOs.Auth
{
    // ✅ USER-DEFINED: Client-side login request model
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
