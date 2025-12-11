namespace CRM.Server.DTOs.Auth
{
    public class RefreshResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshExpiresAt { get; set; }
    }
}
