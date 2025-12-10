using Microsoft.EntityFrameworkCore;
using CRM.Server.Data;
using CRM.Server.Models.Tasks;
using CRM.Server.Repositories;
using CRM.Server.Dtos;


namespace CRM.Server.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _context;

    public TaskRepository(ApplicationDbContext context)
    {
        _context = context;
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
        _context.Tasks.Add(task);
        _context.SaveChanges();
        return task;
    }

    public TaskItem Update(TaskItem task)
    {
        _context.Tasks.Update(task);
        _context.SaveChanges();
        return task;
    }

    public void Delete(Guid id)
    {
        var task = _context.Tasks.FirstOrDefault(x => x.TaskId == id);
        if (task == null)
            throw new Exception("Task not found");

        _context.Tasks.Remove(task);
        _context.SaveChanges();
    }
}
