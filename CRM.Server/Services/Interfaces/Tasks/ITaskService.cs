using CRM.Server.Dtos;

namespace CRM.Server.Services
{
    public interface ITaskService
    {
        IEnumerable<TaskResponseDto> GetAll(TaskFilterDto? filter = null);
        TaskResponseDto? GetById(Guid id);
        TaskResponseDto Create(CreateTaskDto dto);
        TaskResponseDto Update(Guid id, UpdateTaskDto dto);
        void Delete(Guid id);
    }
}
