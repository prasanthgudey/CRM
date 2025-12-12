namespace CRM.Server.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string? Token { get; set; }

        public DateTime? Expiration { get; set; }

        public bool MfaRequired { get; set; } = false;

        public string? Email { get; set; }

        // NEW → Refresh token and expiry
        public string? RefreshToken { get; set; }
        public DateTime? RefreshExpiresAt { get; set; }
    }
}
