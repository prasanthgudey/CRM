using Microsoft.EntityFrameworkCore;
using CRM.Server.Data;
using CRM.Server.Models.Tasks;
using CRM.Server.Dtos;

namespace CRM.Server.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskRepository> _logger;

        public TaskRepository(ApplicationDbContext context, ILogger<TaskRepository> logger)
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
    }
}
