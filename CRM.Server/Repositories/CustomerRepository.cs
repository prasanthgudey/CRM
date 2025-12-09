using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using CRM.Server.Data;


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
    }
}
