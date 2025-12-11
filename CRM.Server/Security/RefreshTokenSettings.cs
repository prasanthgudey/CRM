namespace CRM.Server.Security
{
    public class RefreshTokenSettings
    {
        public int RefreshTokenExpiryDays { get; set; } = 7;
        public bool RotationEnabled { get; set; } = true;
        public int MaxActiveRefreshTokensPerUser { get; set; } = 10;
    }
}
