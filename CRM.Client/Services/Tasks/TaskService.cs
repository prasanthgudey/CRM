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

        // Existing endpoints (keep as-is)
        public async Task<List<TaskResponseDto>?> GetAllAsync()
            => await _api.GetAsync<List<TaskResponseDto>>("api/tasks/all");

        public async Task<TaskResponseDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<TaskResponseDto>($"api/tasks/{id}");

        public async Task<List<TaskResponseDto>?> GetByCustomerIdAsync(Guid customerId)
            => await _api.GetAsync<List<TaskResponseDto>>($"api/tasks/customer/{customerId}");

        public async Task<List<TaskResponseDto>?> GetByUserIdAsync(string userId)
            => await _api.GetAsync<List<TaskResponseDto>>($"api/tasks/user/{userId}");

        public async Task CreateAsync(CreateTaskDto dto)
            => await _api.PostAsync<CreateTaskDto, object>("api/tasks", dto);

        public async Task UpdateAsync(Guid id, UpdateTaskDto dto)
            => await _api.PutAsync<UpdateTaskDto, object>($"api/tasks/{id}", dto);

        public async Task DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/tasks/{id}");

        // -------------------------
        // Dashboard-friendly helpers
        // -------------------------

        /// <summary>
        /// Returns total number of tasks.
        /// Backend route expected: GET /api/tasks/count
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            // endpoint should return an integer (or object with a 'count' property — adjust if needed)
            var result = await _api.GetAsync<int?>("api/tasks/count");
            return result ?? 0;
        }

        /// <summary>
        /// Returns number of open (not completed) tasks.
        /// Backend route expected: GET /api/tasks/open/count
        /// </summary>
        public async Task<int> GetOpenCountAsync()
        {
            var result = await _api.GetAsync<int?>("api/tasks/open/count");
            return result ?? 0;
        }

        /// <summary>
        /// Get recent tasks; optional take parameter (default 50).
        /// Backend route expected: GET /api/tasks/recent?take={take}
        /// </summary>
        public async Task<List<TaskResponseDto>?> GetRecentTasksAsync(int take = 50)
        {
            return await _api.GetAsync<List<TaskResponseDto>>($"api/tasks/recent?take={take}");
        }

        /// <summary>
        /// Larger fetch endpoint for charting if you want more than default recent results.
        /// Backend route expected: GET /api/tasks/recent-for-chart?take={take}
        /// You can call GetRecentTasksAsync(take) instead if your backend doesn't have a specialized route.
        /// </summary>
        public async Task<List<TaskResponseDto>?> GetRecentForChartAsync(int take = 100)
        {
            return await _api.GetAsync<List<TaskResponseDto>>($"api/tasks/recent?take={take}");
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


        // DELETE: /api/tasks/{id}
        //public async Task DeleteAsync(Guid id)
        //{
        //    await _api.DeleteAsync($"api/tasks/{id}");

        //}

    }
}
