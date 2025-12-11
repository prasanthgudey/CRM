using CRM.Server.Common.Paging;
using CRM.Server.Data;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace CRM.Server.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _context;
        public CustomerRepository(ApplicationDbContext context) => _context = context;

        public async Task<List<Customer>> GetAllAsync() => await _context.Customers.ToListAsync();
        public async Task<Customer?> GetByIdAsync(Guid id) => await _context.Customers.FindAsync(id);
        public async Task<Customer> CreateAsync(Customer c)
        { _context.Customers.Add(c); await _context.SaveChangesAsync(); return c; }

        public async Task UpdateAsync(Customer c)
        { _context.Customers.Update(c); await _context.SaveChangesAsync(); }

        public async Task DeleteAsync(Customer c)
        { _context.Customers.Remove(c); await _context.SaveChangesAsync(); }

        public async Task<bool> ExistsAsync(Guid id) => await _context.Customers.AnyAsync(x => x.CustomerId == id);

        public async Task<PagedResult<Customer>> GetPagedAsync(PageParams parms)
        {
            var query = _context.Customers.AsQueryable();

            // Sorting
            if (!string.IsNullOrWhiteSpace(parms.SortBy))
            {
                if (parms.SortBy.Equals("firstName", StringComparison.OrdinalIgnoreCase))
                {
                    query = parms.SortDir == "desc" ? query.OrderByDescending(c => c.FirstName)
                                                    : query.OrderBy(c => c.FirstName);
                }
            }
            else
            {
                query = query.OrderByDescending(c => c.CreatedAt);
            }

            return await query.ToPagedResultAsync(parms.Page, parms.PageSize);
        }
    }
}
