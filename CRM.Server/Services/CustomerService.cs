using CRM.Server.DTOs;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services.Interfaces;
using CRM.Server.Data;


namespace CRM.Server.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repo;
        public CustomerService(ICustomerRepository repo) => _repo = repo;

        public async Task<List<CustomerResponseDto>> GetAllAsync()
            => (await _repo.GetAllAsync()).Select(c => new CustomerResponseDto
            {
                CustomerId = c.CustomerId,
                //FullName = c.FullName,
                FirstName = c.FirstName,
                Surname = c.Surname,
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
                //FullName = c.FullName,
                FirstName = c.FirstName,
                Surname = c.Surname,
                MiddleName = c.MiddleName,
                PreferredName = c.PreferredName,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                CreatedByUserId = c.CreatedByUserId,
                CreatedAt = c.CreatedAt
            };
        }

        public async Task<CustomerResponseDto> CreateAsync(CustomerCreateDto dto)
        {
            var c = new Customer
            {
                CustomerId = Guid.NewGuid(),
                //FullName = dto.FullName,
                FirstName = dto.FirstName,
                Surname = dto.Surname,
                MiddleName = dto.MiddleName,
                PreferredName = dto.PreferredName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                CreatedByUserId = dto.CreatedByUserId,
                CreatedAt = DateTime.UtcNow.ToString("O")
            };
            await _repo.CreateAsync(c);

            return new CustomerResponseDto
            {
                CustomerId = c.CustomerId,
                //FullName = c.FullName,
                FirstName = c.FirstName,
                Surname = c.Surname,
                MiddleName = c.MiddleName,
                PreferredName = c.PreferredName,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                CreatedByUserId = c.CreatedByUserId,
                CreatedAt = c.CreatedAt
            };
        }

        public async Task<bool> UpdateAsync(Guid id, CustomerUpdateDto dto)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c is null) return false;
            //c.FullName = dto.FullName;
            c.FirstName = dto.FirstName;
            c.Surname = dto.Surname;
            c.MiddleName = dto.MiddleName;
            c.PreferredName = dto.PreferredName;
            c.Email = dto.Email;
            c.Phone = dto.Phone;
            c.Address = dto.Address;
            await _repo.UpdateAsync(c);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c is null) return false;
            await _repo.DeleteAsync(c);
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

            // Filter by name across FullName, MiddleName, PreferredName
            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();
                customers = customers
                    .Where(c =>
                        (!string.IsNullOrEmpty(c.FirstName) &&
                            c.FirstName.Contains(name, StringComparison.OrdinalIgnoreCase)) ||
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

            // Global search across all main fields
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                customers = customers
                    .Where(c =>
                        (!string.IsNullOrEmpty(c.FirstName) &&
                            c.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(c.MiddleName) &&
                            c.MiddleName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
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

    }
}
