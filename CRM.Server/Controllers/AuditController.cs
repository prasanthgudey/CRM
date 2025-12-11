using CRM.Server.DTOs.Audit;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditRepository _auditRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuditController(
            IAuditRepository auditRepository,
            UserManager<ApplicationUser> userManager)
        {
            _auditRepository = auditRepository;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLogs()
        {
            var logs = await _auditRepository.GetAllAsync();

            // load users once to avoid N+1 queries
            var users = await _userManager.Users
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync();

            var result = logs.Select(log =>
            {
                var performedBy = !string.IsNullOrWhiteSpace(log.PerformedByUserId)
                    ? users.FirstOrDefault(u => u.Id == log.PerformedByUserId)?.FullName
                    : null;

                var targetUser = !string.IsNullOrWhiteSpace(log.TargetUserId)
                    ? users.FirstOrDefault(u => u.Id == log.TargetUserId)?.FullName
                    : null;

                return new AuditLogResponseDto
                {
                    Id = log.Id,
                    PerformedByUserId = log.PerformedByUserId,
                    PerformedByUserName = performedBy,
                    TargetUserId = log.TargetUserId,
                    TargetUserName = targetUser,
                    Action = log.Action,
                    EntityName = log.EntityName,
                    OldValue = log.OldValue,
                    NewValue = log.NewValue,
                    IpAddress = log.IpAddress,
                    IsSuccess = log.IsSuccess,
                    CreatedAt = log.CreatedAt
                };
            }).ToList();

            return Ok(result);
        }
    }
}
