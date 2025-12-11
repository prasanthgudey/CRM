namespace CRM.Server.Security
{
    public class PasswordPolicySettings
    {
        public int RequiredLength { get; set; }
        public bool RequireUppercase { get; set; }
        public bool RequireLowercase { get; set; }
        public bool RequireDigit { get; set; }
        public bool RequireNonAlphanumeric { get; set; }

        public int PasswordExpiryMinutes { get; set; } = 2;
    }
}
