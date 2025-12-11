using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CRM.Server.Common.Paging
{
    public static class QueryableExtensions
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int pageSize)
            where T : class
        {
            if (page <= 0) page = 1;
            pageSize = System.Math.Clamp(pageSize, 1, 200);

            var total = await query.LongCountAsync();

            // compute total pages
            var totalPages = pageSize == 0 ? 0 : (int)System.Math.Ceiling((double)total / pageSize);

            // If there are no items, return empty result with page=1 (or keep requested page if you prefer)
            if (totalPages == 0)
            {
                return new PagedResult<T>
                {
                    Items = new List<T>(),
                    Page = 1,
                    PageSize = pageSize,
                    TotalCount = total
                };
            }

            // Clamp requested page to range [1, totalPages]
            if (page > totalPages) page = totalPages;

            var skip = (page - 1) * pageSize;
            var items = await query.Skip(skip).Take(pageSize).AsNoTracking().ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }
    }
}
