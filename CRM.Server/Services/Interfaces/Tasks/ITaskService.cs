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

        // -------------------------
        // Dashboard-friendly helpers
        // -------------------------

        /// <summary>
        /// Returns total number of tasks.
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        /// Returns number of open (not completed) tasks.
        /// </summary>
        Task<int> GetOpenCountAsync();

        /// <summary>
        /// Returns recent tasks for dashboards (ordered newest first).
        /// </summary>
        Task<List<TaskResponseDto>> GetRecentTasksAsync(int take = 50);

        //paginig
        Task<PagedResult<TaskResponseDto>> GetPagedAsync(PageParams parms);
    }
}
