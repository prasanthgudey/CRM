namespace CRM.Client.Config
{
    // ✅ This is a USER-DEFINED configuration model
    // ✅ It will be bound at app startup and used by all services
    public class ApiSettings
    {
        // ✅ Your backend base URL (example: https://localhost:7194)
        public string BaseUrl { get; set; } = string.Empty;

        // ✅ Default timeout for all HTTP requests (in seconds)
        public int TimeoutSeconds { get; set; } = 30;

        // ✅ Future-proofing: retry enabled or not
        public bool EnableRetry { get; set; } = false;
    }
}
