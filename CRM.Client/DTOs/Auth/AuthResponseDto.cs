namespace CRM.Client.DTOs.Auth
{
    // ✅ USER-DEFINED: JWT login response model (MFA-aware)
    public class AuthResponseDto
    {
        public string? Token { get; set; }
        public DateTime? Expiration { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshExpiresAt { get; set; }
        public bool MfaRequired { get; set; }
        public string? Email { get; set; }
    }

}
