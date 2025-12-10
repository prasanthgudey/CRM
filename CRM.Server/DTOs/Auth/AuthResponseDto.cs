namespace CRM.Server.DTOs.Auth
{
    public class AuthResponseDto
    {
        // JWT token (present only when MFA not required or after successful MFA)
        public string? Token { get; set; }

        // Token expiration (UTC)
        public DateTime? Expiration { get; set; }

        // When true, client must prompt for OTP and call /api/auth/mfa/login
        public bool MfaRequired { get; set; } = false;

        // Optional: email echoed back to help pre-fill the OTP screen
        public string? Email { get; set; }
    }
}
