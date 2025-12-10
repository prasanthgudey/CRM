using CRM.Server.Dtos;
using CRM.Server.Models.Tasks;


namespace CRM.Server.Repositories;

public interface ITaskRepository
{
    IEnumerable<TaskItem> GetAll();
    TaskItem? GetById(Guid id);
    TaskItem Add(TaskItem task);
    TaskItem Update(TaskItem task);
    void Delete(Guid id);
}
