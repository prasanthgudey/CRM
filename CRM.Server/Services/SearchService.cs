using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services.Interfaces;
using CRM.Server.DTOs.Shared;
using System.Security.Claims;

namespace CRM.Server.Services
{
    public class SearchService : ISearchService
    {
        private readonly ISearchRepository _repo;

        public SearchService(ISearchRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<SearchResultDto>> SearchAsync(string query, int limit, ClaimsPrincipal user)
        {
            // If your app restricts search by user roles, you can check here using "user"

            return await _repo.SearchAsync(query, limit);
        }
    }
}
