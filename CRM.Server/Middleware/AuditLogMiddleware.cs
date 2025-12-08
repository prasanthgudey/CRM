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
            IAuditLogService auditLogService) // ✅ Built-in DI into middleware
        {
            await _next(context);

            // ✅ Only log authenticated users
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var path = context.Request.Path.Value;
                var ip = context.Connection.RemoteIpAddress?.ToString();

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(path))
                {
                    await auditLogService.LogAsync(
                        userId,
                        $"API Accessed: {path}",
                        ip,
                        null
                    );
                }
            }
        }
    }
}
