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

        // Existing endpoint: GET /api/audit
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

        // -------------------------
        // NEW: Dashboard endpoints
        // -------------------------

        /// <summary>
        /// NEW: GET /api/audit/count
        /// Returns total number of audit log entries (int).
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetTotalCount()
        {
            var logs = await _auditRepository.GetAllAsync();
            var count = logs?.Count ?? 0;
            return Ok(count);
        }

        /// <summary>
        /// NEW: GET /api/audit/recent?take=6
        /// Returns recent audit logs (mapped to AuditLogResponseDto).
        /// </summary>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent([FromQuery] int take = 6)
        {
            var logs = await _auditRepository.GetAllAsync();

            // load users once to avoid N+1 queries
            var users = await _userManager.Users
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync();

            var recent = logs
                .OrderByDescending(l => l.CreatedAt)
                .Take(take)
                .Select(log =>
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
                })
                .ToList();

            return Ok(recent);
        }

        /// <summary>
        /// NEW: GET /api/audit/errors?days=7
        /// Returns count of error (IsSuccess == false) audit logs within the last `days`.
        /// </summary>
        [HttpGet("errors")]
        public async Task<IActionResult> GetErrorCount([FromQuery] int days = 7)
        {
            var logs = await _auditRepository.GetAllAsync();
            if (logs == null || logs.Count == 0) return Ok(0);

            var cutoff = DateTime.UtcNow.AddDays(-days);

            var count = logs.Count(l => !l.IsSuccess && l.CreatedAt >= cutoff);
            return Ok(count);
        }
    }
}
