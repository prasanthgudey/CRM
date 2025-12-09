using CRM.Server.Data;              // ✅ DbContext is in Data namespace
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRM.Server.Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(Guid id);
        Task<Customer> CreateAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task DeleteAsync(Customer customer);
        Task<bool> ExistsAsync(Guid id);
    }
}
