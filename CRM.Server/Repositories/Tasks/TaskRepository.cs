using CRM.Server.Common.Paging;
using CRM.Server.Data;
using CRM.Server.Dtos;
using CRM.Server.Models.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CRM.Server.Repositories.Tasks

{
    public class TaskRepository : BaseRepository<TaskItem>, ITaskRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskRepository> _logger;

        public TaskRepository(ApplicationDbContext context, ILogger<TaskRepository> logger):base(context)
        {
            _context = context;
            _logger = logger;
        }

        public IEnumerable<TaskItem> GetAll()
        { 
            return _context.Tasks.AsNoTracking().ToList();
        }

        public TaskItem? GetById(Guid id)
        {
            return _context.Tasks.FirstOrDefault(x => x.TaskId == id);
        }

        public TaskItem Add(TaskItem task)
        {
            try
            {
                _context.Tasks.Add(task);
                _context.SaveChanges();

                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Database error while adding Task with Id {TaskId}", task.TaskId);

                throw;
            }
        }

        public TaskItem Update(TaskItem task)
        {
            try
            {
                _context.Tasks.Update(task);
                _context.SaveChanges();

                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Database error while updating Task with Id {TaskId}", task.TaskId);

                throw;
            }
        }

        public void Delete(Guid id)
        {
            var task = _context.Tasks.FirstOrDefault(x => x.TaskId == id);

            if (task == null)
            {
                _logger.LogWarning("Task with Id {TaskId} not found for deletion", id);
                throw new Exception("Task not found");
            }

            try
            {
                _context.Tasks.Remove(task);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Database error while deleting Task with Id {TaskId}", id);

                throw;
            }
        }
        // Paged implementation uses BaseRepository's GetPagedAsync by passing a customize function
        public async Task<PagedResult<TaskItem>> GetPagedAsync(PageParams parms)
        {
            return await base.GetPagedAsync(parms, q =>
            {
                if (!string.IsNullOrWhiteSpace(parms.Search))
                {
                    var s = parms.Search!.Trim();
                    q = q.Where(t =>
                        EF.Functions.Like(t.Title ?? "", $"%{s}%") ||
                        EF.Functions.Like(t.Description ?? "", $"%{s}%") ||
                        EF.Functions.Like(t.Priority.ToString(), $"%{s}%"));
                }

                // whitelist sorting (safe)
                if (!string.IsNullOrWhiteSpace(parms.SortBy) && parms.SortBy!.Equals("dueDate", StringComparison.OrdinalIgnoreCase))
                {
                    q = parms.SortDir?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true
                        ? q.OrderByDescending(t => t.DueDate)
                        : q.OrderBy(t => t.DueDate);
                }
                else
                {
                    q = q.OrderByDescending(t => t.CreatedAt);
                }

                return q;
            });
        }
    }
}
