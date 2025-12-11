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

        // Updated to accept performer id for auditing
        Task<CustomerResponseDto> CreateAsync(CustomerCreateDto dto, string performedByUserId);

        Task<bool> UpdateAsync(Guid id, CustomerUpdateDto dto, string performedByUserId);

        Task<bool> DeleteAsync(Guid id, string performedByUserId);

        Task<PagedResult<CustomerResponseDto>> GetPagedAsync(PageParams parms);

    }
}
