using CRM.Server.Models.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Server.Extensions
{
    // USER-DEFINED: universal pagination engine for all entities
    public static class QueryableExtensions
    {
        // Convert any IQueryable<TSource> into a paged result of TDto
        public static async Task<PagedResponse<TDto>> ToPagedResponseAsync<TSource, TDto>(
            this IQueryable<TSource> query,
            int pageNumber,
            int pageSize,
            System.Func<IQueryable<TSource>, IQueryable<TDto>> projection,
            CancellationToken cancellationToken = default)
        {
            // 1) Count total items (after filters are applied)
            var total = await query.CountAsync(cancellationToken);

            // 2) Calculate skip
            var skip = (pageNumber - 1) * pageSize;

            // 3) Apply paging to the source query
            var pagedQuery = query
                .Skip(skip)
                .Take(pageSize);

            // 4) Apply projection (convert DB entity -> DTO)
            var projected = projection(pagedQuery);

            // 5) Execute query and return results
            var items = await projected.ToListAsync(cancellationToken);

            return new PagedResponse<TDto>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
