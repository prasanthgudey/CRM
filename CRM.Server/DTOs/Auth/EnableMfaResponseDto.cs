namespace CRM.Server.DTOs.Auth
{
    public class EnableMfaResponseDto
    {
        public string SharedKey { get; set; } = string.Empty;
        public string QrCodeImageUrl { get; set; } = string.Empty;
    }
}
