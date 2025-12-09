using CRM.Server.DTOs;
using CRM.Server.Data;

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
        Task<CustomerResponseDto> CreateAsync(CustomerCreateDto dto);
        Task<bool> UpdateAsync(Guid id, CustomerUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
