namespace CRM.Server.Security
{
    public class SessionSettings
    {
        /// <summary>
        /// Inactivity timeout in minutes — if no authenticated request happens during this period,
        /// the session will be considered expired and the next request will be denied.
        /// </summary>
        public int InactivityTimeoutMinutes { get; set; } = 15;

        /// <summary>
        /// Optional absolute lifetime for a session in minutes (regardless of activity).
        /// Set to 0 to disable absolute lifetime.
        /// </summary>
        public int AbsoluteSessionLifetimeMinutes { get; set; } = 1440;
    }
}
