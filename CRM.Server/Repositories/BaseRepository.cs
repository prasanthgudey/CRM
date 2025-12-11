using CRM.Server.Common.Paging;
using CRM.Server.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CRM.Server.Repositories
{
    public abstract class BaseRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        protected BaseRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        /// <summary>
        /// Generic paged method. Caller can pass a customize func to apply filters and ordering.
        /// Caller should apply ordering inside customize for stable results.
        /// </summary>
        public virtual async Task<PagedResult<T>> GetPagedAsync(PageParams parms, Func<IQueryable<T>, IQueryable<T>>? customize = null)
        {
            var query = _dbSet.AsQueryable();

            if (customize != null)
                query = customize(query);

            return await query.ToPagedResultAsync(parms.Page, parms.PageSize);
        }
    }
}
