namespace CRM.Client.DTOs.Auth
{
    // ✅ USER-DEFINED: JWT login response model (MFA-aware)
    public class AuthResponseDto
    {
        public string? Token { get; set; }

        public DateTime? Expiration { get; set; }

        // ✅ NEW: tells frontend whether OTP is required
        public bool MfaRequired { get; set; }

        // ✅ NEW: used to pass email to /mfa-login page
        public string? Email { get; set; }
    }
}
