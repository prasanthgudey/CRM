using CRM.Server.Dtos;
using CRM.Server.Models.Tasks;
using CRM.Server.Repositories;

namespace CRM.Server.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _repo;

        public TaskService(ITaskRepository repo)
        {
            _repo = repo;
        }

        public IEnumerable<TaskResponseDto> GetAll(TaskFilterDto? filter = null)
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

        public TaskResponseDto? GetById(Guid id)
        {
            var task = _repo.GetById(id);
            return task is null ? null : ToResponseDto(task);
        }

        public TaskResponseDto Create(CreateTaskDto dto)
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
            return ToResponseDto(saved);
        }

        public TaskResponseDto Update(Guid id, UpdateTaskDto dto)
        {
            var existing = _repo.GetById(id)
                ?? throw new Exception("Task not found");

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

            if (dto.IsRecurring.HasValue)
                existing.IsRecurring = dto.IsRecurring.Value;

            if (dto.RecurrenceType.HasValue)
                existing.RecurrenceType = dto.RecurrenceType.Value;

            if (dto.RecurrenceInterval.HasValue)
                existing.RecurrenceInterval = dto.RecurrenceInterval;

            if (dto.RecurrenceEndDate.HasValue)
                existing.RecurrenceEndDate = dto.RecurrenceEndDate;

            var updated = _repo.Update(existing);
            return ToResponseDto(updated);
        }

        public void Delete(Guid id)
        {
            _repo.Delete(id);
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
