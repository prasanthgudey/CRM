using CRM.Server.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditRepository _auditRepository;

        // ✅ Repository used for read-only fetch
        public AuditController(IAuditRepository auditRepository)
        {
            _auditRepository = auditRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLogs()
        {
            var logs = await _auditRepository.GetAllAsync();
            return Ok(logs);
        }
    }
}
