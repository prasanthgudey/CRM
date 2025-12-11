using CRM.Server.Services.Interfaces;
using System.Security.Claims;

namespace CRM.Server.Middleware
{
    // ✅ Custom audit logging middleware
    public class AuditLogMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditLogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IAuditLogService auditLogService) // ✅ Injected correctly
        {

            Console.WriteLine("✅ AUDIT MIDDLEWARE HIT");

            await _next(context);

            // ✅ Only log authenticated users
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var performedByUserId =
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var path = context.Request.Path.Value;
                var ip = context.Connection.RemoteIpAddress?.ToString();

                if (!string.IsNullOrWhiteSpace(performedByUserId) &&
                    !string.IsNullOrWhiteSpace(path))
                {
                    await auditLogService.LogAsync(
                        performedByUserId,     // ✅ Who triggered
                        null,                  // ✅ No target user for middleware logs
                        $"API Accessed: {path}",
                        "API",
                        true,                  // ✅ Success assumed here
                        ip,                    // ✅ IP Address
                        null,                  // ✅ OldValue
                        null                   // ✅ NewValue
                    );
                }
            }
        }
    }
}
