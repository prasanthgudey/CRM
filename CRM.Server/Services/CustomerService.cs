using CRM.Server.Common.Paging;
using CRM.Server.DTOs;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace CRM.Server.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repo;
        private readonly IAuditLogService _auditLogService;

        public CustomerService(ICustomerRepository repo, IAuditLogService auditLogService)
        {
            _repo = repo;
            _auditLogService = auditLogService;
        }

        public async Task<List<CustomerResponseDto>> GetAllAsync()
            => (await _repo.GetAllAsync()).Select(c => new CustomerResponseDto
            {
                CustomerId = c.CustomerId,
                FirstName = c.FirstName,
                SurName = c.SurName,
                MiddleName = c.MiddleName,
                PreferredName = c.PreferredName,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                CreatedByUserId = c.CreatedByUserId,
                CreatedAt = c.CreatedAt
            }).ToList();

        public async Task<CustomerResponseDto?> GetByIdAsync(Guid id)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c is null) return null;
            return new CustomerResponseDto
            {
                CustomerId = c.CustomerId,
                FirstName = c.FirstName,
                SurName = c.SurName,
                MiddleName = c.MiddleName,
                PreferredName = c.PreferredName,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                CreatedByUserId = c.CreatedByUserId,
                CreatedAt = c.CreatedAt
            };
        }

        // CREATE with audit
        public async Task<CustomerResponseDto> CreateAsync(CustomerCreateDto dto, string performedByUserId)
        {
            var c = new Customer
            {
                CustomerId = Guid.NewGuid(),
                FirstName = dto.FirstName,
                SurName = dto.SurName,
                MiddleName = dto.MiddleName,
                PreferredName = dto.PreferredName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                CreatedByUserId = dto.CreatedByUserId,
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            await _repo.CreateAsync(c);

            // Audit: Created
            await SafeAudit(
                performedByUserId,
                c.CustomerId.ToString(),
                "Customer Created",
                "Customer",
                null,
                JsonSerializer.Serialize(c)
            );

            return new CustomerResponseDto
            {
                CustomerId = c.CustomerId,
                FirstName = c.FirstName,
                SurName = c.SurName,
                MiddleName = c.MiddleName,
                PreferredName = c.PreferredName,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                CreatedByUserId = c.CreatedByUserId,
                CreatedAt = c.CreatedAt
            };
        }

        // UPDATE with old/new audit
        public async Task<bool> UpdateAsync(Guid id, CustomerUpdateDto dto, string performedByUserId)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c is null) return false;

            var oldValue = JsonSerializer.Serialize(c);

            c.FirstName = dto.FirstName;
            c.SurName = dto.SurName;
            c.MiddleName = dto.MiddleName;
            c.PreferredName = dto.PreferredName;
            c.Email = dto.Email;
            c.Phone = dto.Phone;
            c.Address = dto.Address;

            await _repo.UpdateAsync(c);

            var newValue = JsonSerializer.Serialize(c);

            await SafeAudit(
                performedByUserId,
                c.CustomerId.ToString(),
                "Customer Updated",
                "Customer",
                oldValue,
                newValue
            );

            return true;
        }

        // DELETE with audit
        public async Task<bool> DeleteAsync(Guid id, string performedByUserId)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c is null) return false;

            var oldValue = JsonSerializer.Serialize(c);

            await _repo.DeleteAsync(c);

            await SafeAudit(
                performedByUserId,
                c.CustomerId.ToString(),
                "Customer Deleted",
                "Customer",
                oldValue,
                null
            );

            return true;
        }

        public async Task<List<CustomerResponseDto>> FilterAsync(
            string? name,
            string? email,
            string? phone,
            string? address,
            string? search)
        {
            var customers = await _repo.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();
                customers = customers
                    .Where(c =>
                        (!string.IsNullOrEmpty(c.FirstName) &&
                            c.FirstName.Contains(name, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(c.SurName) &&
                            c.SurName.Contains(name, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(c.MiddleName) &&
                            c.MiddleName.Contains(name, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(c.PreferredName) &&
                            c.PreferredName.Contains(name, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                email = email.Trim();
                customers = customers
                    .Where(c => !string.IsNullOrEmpty(c.Email) &&
                                c.Email.Contains(email, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                phone = phone.Trim();
                customers = customers
                    .Where(c => !string.IsNullOrEmpty(c.Phone) &&
                                c.Phone.Contains(phone))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(address))
            {
                address = address.Trim();
                customers = customers
                    .Where(c => !string.IsNullOrEmpty(c.Address) &&
                                c.Address.Contains(address, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                customers = customers
                    .Where(c =>
                        (!string.IsNullOrEmpty(c.FirstName) &&
                            c.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(c.MiddleName) &&
                            c.MiddleName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(c.SurName) &&
                            c.SurName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(c.PreferredName) &&
                            c.PreferredName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(c.Email) &&
                            c.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(c.Phone) &&
                            c.Phone.Contains(search)) ||
                        (!string.IsNullOrEmpty(c.Address) &&
                            c.Address.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            return customers
                .Select(c => new CustomerResponseDto
                {
                    CustomerId = c.CustomerId,
                    FirstName = c.FirstName,
                    SurName = c.SurName,
                    MiddleName = c.MiddleName,
                    PreferredName = c.PreferredName,
                    Email = c.Email,
                    Phone = c.Phone,
                    Address = c.Address,
                    CreatedByUserId = c.CreatedByUserId,
                    CreatedAt = c.CreatedAt
                })
                .ToList();
        }

        // SAFE AUDIT helper
        private async Task SafeAudit(
            string? performedByUserId,
            string? targetId,
            string action,
            string entityName,
            string? oldValue,
            string? newValue)
        {
            try
            {
                await _auditLogService.LogAsync(
                    performedByUserId,
                    targetId,
                    action,
                    entityName,
                    true,
                    null,
                    oldValue,
                    newValue
                );
            }
            catch
            {
                // swallow - auditing must not break main flow
            }
        }

        public async Task<PagedResult<CustomerResponseDto>> GetPagedAsync(PageParams parms)
        {
            var paged = await _repo.GetPagedAsync(parms);

            return new PagedResult<CustomerResponseDto>
            {
                Items = paged.Items.Select(c => new CustomerResponseDto
                {
                    CustomerId = c.CustomerId,
                    FirstName = c.FirstName,
                    SurName = c.SurName,
                    MiddleName = c.MiddleName,
                    PreferredName = c.PreferredName,
                    Email = c.Email,
                    Phone = c.Phone,
                    Address = c.Address,
                    CreatedByUserId = c.CreatedByUserId,
                    CreatedAt = c.CreatedAt
                }).ToList(),

                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };
        }
    }
}
