namespace CRM.Client.DTOs.Auth
{
    public class EnableMfaResponseDto
    {
        public string SharedKey { get; set; } = string.Empty;
        public string QrCodeImageUrl { get; set; } = string.Empty; // otpauth:// URI
    }
}
