using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize] // or AllowAnonymous during testing
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 25)
    {
        var results = await _searchService.SearchAsync(q, limit, User);
        return Ok(results);
    }
}
