using CRM.Server.DTOs.Shared;
using CRM.Server.Data;
using CRM.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRM.Server.Repositories
{
    public class SearchRepository : ISearchRepository
    {
        private readonly ApplicationDbContext _db;
        public SearchRepository(ApplicationDbContext db) => _db = db;

        public async Task<List<SearchResultDto>> SearchAsync(string query, int limit = 25)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<SearchResultDto>();

            query = query.Trim();

            // CUSTOMER QUERY (CreatedAt kept as string for in-memory parse)
            var customers = _db.Customers
                .Where(c =>
                       (c.FirstName != null && EF.Functions.Like(c.FirstName, $"%{query}%"))
                    || (c.SurName != null && EF.Functions.Like(c.SurName, $"%{query}%"))
                    || (c.PreferredName != null && EF.Functions.Like(c.PreferredName, $"%{query}%"))
                    || (c.Email != null && EF.Functions.Like(c.Email, $"%{query}%"))
                    || (c.Phone != null && EF.Functions.Like(c.Phone, $"%{query}%"))
                )
                .Select(c => new
                {
                    EntityType = "Customer",
                    Id = c.CustomerId.ToString(),
                    Title = !string.IsNullOrEmpty(c.FirstName) && !string.IsNullOrEmpty(c.SurName)
                                ? (c.FirstName + " " + c.SurName)
                                : (c.PreferredName ?? c.FirstName ?? string.Empty),
                    Subtitle = c.Email ?? c.Phone ?? string.Empty,
                    Snippet = c.Address != null && c.Address.Length > 200
                                ? c.Address.Substring(0, 200)
                                : c.Address,
                    Route = $"/customers/{c.CustomerId}",
                    CreatedAtString = c.CreatedAt
                })
                .AsNoTracking();

            // TASK QUERY (TaskItem)
            var tasks = _db.Tasks
                .Where(t =>
                       (t.Title != null && EF.Functions.Like(t.Title, $"%{query}%"))
                    || (t.Description != null && EF.Functions.Like(t.Description, $"%{query}%"))
                )
                .Select(t => new
                {
                    EntityType = "Task",
                    Id = t.TaskId.ToString(),
                    Title = t.Title,
                    Subtitle = (t.Priority != null ? t.Priority.ToString() : "") + (t.State != null ? $" | {t.State}" : ""),
                    Snippet = t.Description != null && t.Description.Length > 200
                                ? t.Description.Substring(0, 200)
                                : t.Description,
                    Route = $"/tasks/{t.TaskId}",
                    Date = (DateTime?)t.CreatedAt
                })
                .AsNoTracking();

            // USER QUERY (supports full-name searching)
            var userQuery = _db.Users.AsQueryable();

            // Split incoming query into tokens for partial matches
            var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var raw in tokens)
            {
                var token = raw.Trim();
                if (string.IsNullOrEmpty(token)) continue;

                userQuery = userQuery.Where(u =>
                       (u.FullName != null && EF.Functions.Like(u.FullName, $"%{token}%"))
                    || (u.UserName != null && EF.Functions.Like(u.UserName, $"%{token}%"))
                    || (u.Email != null && EF.Functions.Like(u.Email, $"%{token}%"))
                );
            }

            // Project into anonymous DTOs
            var users = userQuery
                .Select(u => new
                {
                    EntityType = "User",
                    Id = u.Id.ToString(),

                    // Title matches what GlobalSearch highlights
                    Title = !string.IsNullOrWhiteSpace(u.FullName)
                                ? u.FullName
                                : (!string.IsNullOrWhiteSpace(u.UserName)
                                    ? u.UserName
                                    : u.Email ?? string.Empty),

                    Subtitle = u.Email ?? string.Empty,
                    Snippet = (string?)null,

                    // Your route for viewing users
                    Route = $"/admin/users/{u.Id}",

                    Date = (DateTime?)u.CreatedAt
                })
                .AsNoTracking();



            // Materialize queries
            var customerList = await customers.ToListAsync();
            var taskList = await tasks.ToListAsync();
            var userList = await users.ToListAsync();

            // Project customers (parse CreatedAtString in-memory)
            var customerResults = customerList.Select(c => new SearchResultDto
            {
                EntityType = c.EntityType,
                Id = c.Id,
                Title = c.Title,
                Subtitle = c.Subtitle,
                Snippet = c.Snippet,
                Route = c.Route,
                Date = !string.IsNullOrEmpty(c.CreatedAtString) && DateTime.TryParse(c.CreatedAtString, out var dt)
                        ? dt
                        : (DateTime?)null
            });

            // Project tasks
            var taskResults = taskList.Select(t => new SearchResultDto
            {
                EntityType = t.EntityType,
                Id = t.Id,
                Title = t.Title,
                Subtitle = t.Subtitle,
                Snippet = t.Snippet,
                Route = t.Route,
                Date = t.Date
            });

            // Project users
            var userResults = userList.Select(u => new SearchResultDto
            {
                EntityType = u.EntityType,
                Id = u.Id,
                Title = u.Title,
                Subtitle = u.Subtitle,
                Snippet = u.Snippet,
                Route = u.Route,
                Date = u.Date
            });

            // Combine, order, take limit
            var combined = customerResults
                .Concat(taskResults)
                .Concat(userResults)
                .OrderByDescending(r => r.Date ?? DateTime.MinValue)
                .Take(limit)
                .ToList();

            return combined;
        }
    }
}
