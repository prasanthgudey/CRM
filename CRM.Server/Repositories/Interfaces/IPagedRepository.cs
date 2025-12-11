using System.Threading;
using System.Threading.Tasks;
using CRM.Server.Models.Pagination;

namespace CRM.Server.Repositories.Interfaces
{
    // USER-DEFINED: generic contract for paged repositories
    // TDto is the DTO type returned to the client (UserDto, TaskDto, CustomerDto, etc.)
    public interface IPagedRepository<TDto>
    {
        /// <summary>
        /// Return a PagedResponse of TDto for the requested page.
        /// Implementations should apply filters/sorting before calling the shared pagination extension.
        /// </summary>
        Task<PagedResponse<TDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default);
    }
}
