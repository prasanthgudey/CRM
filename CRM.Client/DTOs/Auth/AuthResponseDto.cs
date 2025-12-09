// Backend returns:
// return Ok(new AuthResponseDto
// {
//     Token = token,
//     Expiration = DateTime.UtcNow.AddMinutes(30)
// });

namespace CRM.Client.DTOs.Auth
{
    // ✅ USER-DEFINED: JWT login response model
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;

        public DateTime Expiration { get; set; }
    }
}
