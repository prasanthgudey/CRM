using CRM.Server.Dtos;
using CRM.Server.Models.Tasks;
using CRM.Server.Repositories;

namespace CRM.Server.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _repo;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ITaskRepository repo, ILogger<TaskService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public IEnumerable<TaskResponseDto> GetAll(TaskFilterDto? filter = null)
        {
            _logger.LogInformation("Fetching all tasks with filter: {@Filter}", filter);

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
            }

            var result = query
                .Select(t => ToResponseDto(t))
                .ToList();

            _logger.LogInformation("Returned {Count} tasks", result.Count);

            return result;
        }

        public TaskResponseDto? GetById(Guid id)
        {
            _logger.LogInformation("Fetching task by Id {Id}", id);

            var task = _repo.GetById(id);

            if (task == null)
            {
                _logger.LogWarning("Task with Id {Id} not found", id);
            }

            return task is null ? null : ToResponseDto(task);
        }

        public TaskResponseDto Create(CreateTaskDto dto)
        {
            _logger.LogInformation("Creating task for Customer {CustomerId} by User {UserId}", dto.CustomerId, dto.UserId);

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

            _logger.LogInformation("Task created with Id {TaskId}", saved.TaskId);

            return ToResponseDto(saved);
        }

        public TaskResponseDto Update(Guid id, UpdateTaskDto dto)
        {
            _logger.LogInformation("Updating task {Id}", id);

            var existing = _repo.GetById(id)
                ?? throw new Exception("Task not found");

            if (dto.DueDate.HasValue && dto.DueDate.Value < DateTime.Today)
            {
                _logger.LogWarning("Invalid DueDate {DueDate} for task {Id}", dto.DueDate.Value, id);
                throw new Exception("Due date cannot be in the past.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Title))
                existing.Title = dto.Title;

            if (dto.Description is not null)
                existing.Description = dto.Description;

            if (dto.DueDate.HasValue)
                existing.DueDate = dto.DueDate.Value;

            if (dto.Priority.HasValue)
                existing.Priority = dto.Priority.Value;

            if (dto.State.HasValue)
                existing.State = dto.State.Value;

            if (dto.IsRecurring.HasValue)
                existing.IsRecurring = dto.IsRecurring.Value;

            if (dto.RecurrenceType.HasValue)
                existing.RecurrenceType = dto.RecurrenceType.Value;

            if (dto.RecurrenceInterval.HasValue)
                existing.RecurrenceInterval = dto.RecurrenceInterval;

            if (dto.RecurrenceEndDate.HasValue)
                existing.RecurrenceEndDate = dto.RecurrenceEndDate;

            var updated = _repo.Update(existing);

            _logger.LogInformation("Task {Id} updated successfully", id);

            return ToResponseDto(updated);
        }

        public void Delete(Guid id)
        {
            _logger.LogInformation("Deleting task {Id}", id);

            _repo.Delete(id);

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
    }
}
