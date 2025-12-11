using CRM.Server.Common.Paging;
using CRM.Server.Dtos;

namespace CRM.Server.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskResponseDto>> GetAllAsync(TaskFilterDto? filter = null);

        Task<TaskResponseDto?> GetByIdAsync(Guid id);

        // ✅ Audit-enabled signatures
        Task<TaskResponseDto> CreateAsync(CreateTaskDto dto, string performedByUserId);

        Task<TaskResponseDto> UpdateAsync(Guid id, UpdateTaskDto dto, string performedByUserId);

        Task DeleteAsync(Guid id, string performedByUserId);

        //paginig
        Task<PagedResult<TaskResponseDto>> GetPagedAsync(PageParams parms);
    }
}
