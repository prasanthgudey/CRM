// File: CRM.Client/Services/Customers/CustomerService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRM.Client.DTOs.Customers;
using CRM.Client.DTOs.Shared;
using CRM.Client.Services.Http;

namespace CRM.Client.Services.Customers
{
    public class CustomerService
    {
        private readonly ApiClientService _api;

        public CustomerService(ApiClientService api)
        {
            _api = api;
        }

        // GET: api/customers (with optional filters)
        public async Task<List<CustomerResponseDto>?> GetAllAsync(
            string? name = null,
            string? email = null,
            string? phone = null,
            string? address = null,
            string? search = null)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(name))
                queryParams.Add($"name={Uri.EscapeDataString(name)}");

            if (!string.IsNullOrWhiteSpace(email))
                queryParams.Add($"email={Uri.EscapeDataString(email)}");

            if (!string.IsNullOrWhiteSpace(phone))
                queryParams.Add($"phone={Uri.EscapeDataString(phone)}");

            if (!string.IsNullOrWhiteSpace(address))
                queryParams.Add($"address={Uri.EscapeDataString(address)}");

            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            var url = "api/customers";
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            return await _api.GetAsync<List<CustomerResponseDto>>(url);
        }

        // GET: api/customers/{id}
        public async Task<CustomerResponseDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<CustomerResponseDto>($"api/customers/{id}");

        // POST: api/customers
        public async Task<CustomerResponseDto?> CreateAsync(CustomerCreateDto dto)
            => await _api.PostAsync<CustomerCreateDto, CustomerResponseDto>("api/customers", dto);

        // PUT: api/customers/{id}
        public async Task UpdateAsync(Guid id, CustomerUpdateDto dto)
            => await _api.PutAsync<CustomerUpdateDto, object?>($"api/customers/{id}", dto);

        // DELETE: api/customers/{id}
        public async Task DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/customers/{id}");

        // -------------------------
        // NEW: Dashboard-friendly helpers
        // -------------------------

        /// <summary>
        /// NEW: Returns total number of customers.
        /// Calls GET /api/customers/count
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            var result = await _api.GetAsync<int?>("api/customers/count");
            return result ?? 0;
        }

        /// <summary>
        /// NEW: Returns number of customers created in the last `days`.
        /// Calls GET /api/customers/new?days={days}
        /// This exact name matches the call in your dashboard: GetNewCountAsync
        /// </summary>
        public async Task<int> GetNewCountAsync(int days = 7)   // <-- NOTE: name matches dashboard
        {
            var result = await _api.GetAsync<int?>($"api/customers/new?days={days}");
            return result ?? 0;
        }
        public async Task<PagedResult<CustomerResponseDto>?> GetPagedAsync(
    int page = 1,
    int pageSize = 20)
        {
            var url = $"api/customers/paged?page={page}&pageSize={pageSize}";
            return await _api.GetAsync<PagedResult<CustomerResponseDto>>(url);
        }

    }
}
