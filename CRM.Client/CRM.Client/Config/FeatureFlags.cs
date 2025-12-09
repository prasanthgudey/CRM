namespace CRM.Client.Config
{
    // ✅ USER-DEFINED class for feature visibility control
    public class FeatureFlags
    {
        // ✅ Core modules (already implemented or in progress)
        public bool EnableUserManagement { get; set; } = true;
        public bool EnableRoleManagement { get; set; } = true;
        public bool EnableAuditLogs { get; set; } = true;

        // ✅ Future teammate modules (disabled for now)
        public bool EnableTasksModule { get; set; } = false;
        public bool EnableCustomersModule { get; set; } = false;
        public bool EnableAppointmentsModule { get; set; } = false;
        public bool EnableDocumentsModule { get; set; } = false;
    }
}
