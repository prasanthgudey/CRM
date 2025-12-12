using CRM.Server.DTOs.Shared;

namespace CRM.Server.Repositories.Interfaces
{
    public interface ISearchRepository
    {
        Task<List<SearchResultDto>> SearchAsync(string query, int limit = 25);
    }
}
