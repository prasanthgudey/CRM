using CRM.Client.DTOs.Shared;
using CRM.Client.DTOs.Tasks;
using CRM.Client.Services.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CRM.Client.Services.Tasks
{
    public class TaskService
    {
        private readonly ApiClientService _api;

        public TaskService(ApiClientService api)
        {
            _api = api;
        }

        // GET: /api/tasks/all
        public async Task<List<TaskResponseDto>?> GetAllAsync()
        {
            return await _api.GetAsync<List<TaskResponseDto>>("api/tasks/all");
        }

        // GET: /api/tasks/{id}
        public async Task<TaskResponseDto?> GetByIdAsync(Guid id)
        {
            return await _api.GetAsync<TaskResponseDto>($"api/tasks/{id}");
        }

        // GET: /api/tasks/customer/{customerId}
        public async Task<List<TaskResponseDto>?> GetByCustomerIdAsync(Guid customerId)
        {
            return await _api.GetAsync<List<TaskResponseDto>>($"api/tasks/customer/{customerId}");
        }

        // GET: /api/tasks/user/{userId}
        public async Task<List<TaskResponseDto>?> GetByUserIdAsync(string userId)
        {
            return await _api.GetAsync<List<TaskResponseDto>>($"api/tasks/user/{userId}");
        }

        // POST: /api/tasks
        public async Task CreateAsync(CreateTaskDto dto)
        {
            await _api.PostAsync<CreateTaskDto, object>("api/tasks", dto);
        }

        // PUT: /api/tasks/{id}
        public async Task UpdateAsync(Guid id, UpdateTaskDto dto)
        {
            await _api.PutAsync<UpdateTaskDto, object>($"api/tasks/{id}", dto);
        }

        // DELETE: /api/tasks/{id}
        public async Task DeleteAsync(Guid id)
        {
            await _api.DeleteAsync($"api/tasks/{id}");

        }
         // 🔽 ADD THIS METHOD (keep your existing methods too)
        public async Task<PagedResult<TaskResponseDto>?> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? search = null,
            string? sortBy = null,
            string? sortDir = null)
        {
            var q = new List<string> { $"page={page}", $"pageSize={pageSize}" };

            if (!string.IsNullOrWhiteSpace(search))
                q.Add($"search={Uri.EscapeDataString(search)}");

            if (!string.IsNullOrWhiteSpace(sortBy))
                q.Add($"sortBy={Uri.EscapeDataString(sortBy)}");

            if (!string.IsNullOrWhiteSpace(sortDir))
                q.Add($"sortDir={Uri.EscapeDataString(sortDir)}");

            // Your swagger shows /api/Tasks (capital T) – ASP.NET is case-insensitive,
            // but we'll match it exactly:
            var url = "api/Tasks/paged";

            if (q.Count > 0)
                url += "?" + string.Join("&", q);

            return await _api.GetAsync<PagedResult<TaskResponseDto>>(url);
        }

    }
}
