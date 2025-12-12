using CRM.Server.DTOs.Shared;
using System.Security.Claims;

namespace CRM.Server.Services.Interfaces
{
    public interface ISearchService
    {
        Task<List<SearchResultDto>> SearchAsync(string query, int limit, ClaimsPrincipal user);
    }
}
