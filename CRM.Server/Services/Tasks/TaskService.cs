using CRM.Server.Dtos;
using CRM.Server.Models.Tasks;
using CRM.Server.Repositories;
using CRM.Server.Services.Interfaces;
using System.Text.Json;

namespace CRM.Server.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _repo;
        private readonly ILogger<TaskService> _logger;
        private readonly IAuditLogService _auditLogService;

        public TaskService(ITaskRepository repo, ILogger<TaskService> logger, IAuditLogService auditLogService)
        {
            _repo = repo;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        public async Task<IEnumerable<TaskResponseDto>> GetAllAsync(TaskFilterDto? filter = null)
        {
            var query = _repo.GetAll().AsQueryable();

            if (filter is not null)
            {
                if (filter.CustomerId != Guid.Empty)
                    query = query.Where(t => t.CustomerId == filter.CustomerId);

                if (!string.IsNullOrWhiteSpace(filter.UserId))
                    query = query.Where(t => t.CreatedByUserId == filter.UserId);

                if (filter.Priority.HasValue)
                    query = query.Where(t => t.Priority == filter.Priority.Value);

                if (filter.State.HasValue)
                    query = query.Where(t => t.State == filter.State.Value);

                if (filter.DueFrom.HasValue)
                    query = query.Where(t => t.DueDate >= filter.DueFrom.Value);

                if (filter.DueTo.HasValue)
                    query = query.Where(t => t.DueDate <= filter.DueTo.Value);

                var sortBy = filter.SortBy?.ToLower();
                var direction = (filter.SortDirection ?? "asc").ToLower();

                query = (sortBy, direction) switch
                {
                    ("priority", "desc") => query.OrderByDescending(t => t.Priority),
                    ("priority", _) => query.OrderBy(t => t.Priority),

                    ("status", "desc") => query.OrderByDescending(t => t.State),
                    ("status", _) => query.OrderBy(t => t.State),

                    ("date", "desc") => query.OrderByDescending(t => t.DueDate),
                    ("date", _) => query.OrderBy(t => t.DueDate),

                    _ => query.OrderBy(t => t.TaskId)
                };
            }

            return query
                .Select(t => ToResponseDto(t))
                .ToList();
        }

        public async Task<TaskResponseDto?> GetByIdAsync(Guid id)
        {
            var task = _repo.GetById(id);
            return task is null ? null : ToResponseDto(task);
        }

        // ============================================================
        // ✅ CREATE TASK + AUDIT
        // ============================================================
        public async Task<TaskResponseDto> CreateAsync(CreateTaskDto dto, string performedByUserId)
        {
            var task = new TaskItem
            {
                TaskId = Guid.NewGuid(),
                CustomerId = dto.CustomerId,
                CreatedByUserId = dto.UserId,
                Title = dto.Title,
                Description = dto.Description,
                DueDate = dto.DueDate,
                Priority = dto.Priority,
                State = dto.State,
                CreatedAt = DateTime.UtcNow,
                IsRecurring = dto.IsRecurring,
                RecurrenceType = dto.RecurrenceType,
                RecurrenceInterval = dto.RecurrenceInterval,
                RecurrenceEndDate = dto.RecurrenceEndDate
            };

            if (task.State == TaskState.Completed)
                task.CompletedAt = DateTime.UtcNow;

            var saved = _repo.Add(task);

            await SafeAudit(
                performedByUserId,
                saved.TaskId.ToString(),
                "Task Created",
                "Task",
                null,
                JsonSerializer.Serialize(saved)
            );


            _logger.LogInformation("Task created with Id {TaskId}", saved.TaskId);

            return ToResponseDto(saved);
        }

        // ============================================================
        // ✅ UPDATE TASK + STATUS CHANGE AUDIT
        // ============================================================
        public async Task<TaskResponseDto> UpdateAsync(Guid id, UpdateTaskDto dto, string performedByUserId)
        {
            var existing = _repo.GetById(id)
                ?? throw new Exception("Task not found");

            if (dto.DueDate.HasValue && dto.DueDate.Value < DateTime.Today)
            {
                _logger.LogWarning("Invalid DueDate {DueDate} for task {Id}", dto.DueDate.Value, id);
                throw new Exception("Due date cannot be in the past.");
            }

            var oldValue = JsonSerializer.Serialize(existing);
            var oldState = existing.State;

            if (!string.IsNullOrWhiteSpace(dto.Title))
                existing.Title = dto.Title;

            if (dto.Description is not null)
                existing.Description = dto.Description;

            if (dto.DueDate.HasValue)
            {
                if (dto.DueDate.Value < DateTime.Today)
                    throw new Exception("Due date cannot be in the past.");

                existing.DueDate = dto.DueDate.Value;
            }

            if (dto.Priority.HasValue)
                existing.Priority = dto.Priority.Value;

            if (dto.State.HasValue)
                existing.State = dto.State.Value;

            if (existing.State == TaskState.Completed)
                existing.CompletedAt = DateTime.UtcNow;

            if (dto.IsRecurring.HasValue)
                existing.IsRecurring = dto.IsRecurring.Value;

            if (dto.RecurrenceType.HasValue)
                existing.RecurrenceType = dto.RecurrenceType.Value;

            if (dto.RecurrenceInterval.HasValue)
                existing.RecurrenceInterval = dto.RecurrenceInterval;

            if (dto.RecurrenceEndDate.HasValue)
                existing.RecurrenceEndDate = dto.RecurrenceEndDate;

            var updated = _repo.Update(existing);

            var newValue = JsonSerializer.Serialize(updated);

            // ✅ Status Change separate audit
            if (dto.State.HasValue && oldState != updated.State)
            {
                await SafeAudit(
                    performedByUserId,
                    updated.TaskId.ToString(),
                    "Task Status Changed",
                    "Task",
                    JsonSerializer.Serialize(new { oldState }),
                    JsonSerializer.Serialize(new { updated.State })
                );
            }
            else
            {
                await SafeAudit(
                    performedByUserId,
                    updated.TaskId.ToString(),
                    "Task Updated",
                    "Task",
                    oldValue,
                    newValue
                );
            }


            _logger.LogInformation("Task {Id} updated successfully", id);

            return ToResponseDto(updated);
        }

        // ============================================================
        // ✅ DELETE TASK + AUDIT
        // ============================================================
        public async Task DeleteAsync(Guid id, string performedByUserId)
        {
            _logger.LogInformation("Deleting task {Id}", id);

            var task = _repo.GetById(id)
                ?? throw new Exception("Task not found");

            var oldValue = JsonSerializer.Serialize(task);

            _repo.Delete(id);

            await SafeAudit(
                performedByUserId,
                id.ToString(),
                "Task Deleted",
                "Task",
                oldValue,
                null
            );

            _logger.LogInformation("Task {Id} deleted successfully", id);
        }

        private static TaskResponseDto ToResponseDto(TaskItem t) =>
            new TaskResponseDto
            {
                TaskId = t.TaskId,
                CustomerId = t.CustomerId,
                UserId = t.CreatedByUserId,
                Title = t.Title,
                Description = t.Description,
                DueDate = t.DueDate,
                Priority = t.Priority,
                State = t.State,
                CreatedAt = t.CreatedAt,
                CompletedAt = t.CompletedAt,
                IsRecurring = t.IsRecurring,
                RecurrenceType = t.RecurrenceType,
                RecurrenceInterval = t.RecurrenceInterval,
                RecurrenceEndDate = t.RecurrenceEndDate
            };

        // ============================================================
        // ✅ SAFE AUDIT WRAPPER
        // ============================================================
        private async Task SafeAudit(
            string performedByUserId,
            string targetId,
            string action,
            string entityName,
            string? oldValue,
            string? newValue)
        {
            try
            {
                await _auditLogService.LogAsync(
                    performedByUserId,
                    targetId,
                    action,
                    entityName,
                    true,
                    null,
                    oldValue,
                    newValue
                );
            }
            catch
            {
                // ✅ Never break core task functionality
            }
        }
    }
}
