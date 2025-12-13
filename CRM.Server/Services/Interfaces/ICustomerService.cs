using CRM.Server.Common.Paging;
using CRM.Server.Data;
using CRM.Server.DTOs;

namespace CRM.Server.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<List<CustomerResponseDto>> FilterAsync(
            string? name,
            string? email,
            string? phone,
            string? address,
            string? search);

        Task<CustomerResponseDto?> GetByIdAsync(Guid id);
        Task<List<CustomerResponseDto>> GetAllAsync();



        // Updated to accept performer id for auditing
        Task<CustomerResponseDto> CreateAsync(CustomerCreateDto dto, string performedByUserId);

        Task<bool> UpdateAsync(Guid id, CustomerUpdateDto dto, string performedByUserId);

        Task<bool> DeleteAsync(Guid id, string performedByUserId);

        // -------------------------
        // NEW: Dashboard-friendly helpers
        // -------------------------

        /// <summary>
        /// Returns total number of customers.
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        /// Returns number of customers created within the last `days`.
        /// </summary>
        Task<int> GetNewCustomersCountAsync(int days = 7);


        Task<PagedResult<CustomerResponseDto>> GetPagedAsync(PageParams parms);

    }
}
