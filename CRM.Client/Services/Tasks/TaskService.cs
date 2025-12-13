using CRM.Client.DTOs.Shared;
using CRM.Client.DTOs.Tasks;
using CRM.Client.DTOs.Users;
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

        // =========================
        // READ OPERATIONS (UNCHANGED)
        // =========================
        public async Task<List<TaskResponseDto>?> GetAllAsync()
            => await _api.GetAsync<List<TaskResponseDto>>("api/tasks/all");

        public async Task<TaskResponseDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<TaskResponseDto>($"api/tasks/{id}");

        public async Task<List<TaskResponseDto>?> GetByCustomerIdAsync(Guid customerId)
            => await _api.GetAsync<List<TaskResponseDto>>($"api/tasks/customer/{customerId}");

        public async Task<List<TaskResponseDto>?> GetByUserIdAsync(string userId)
            => await _api.GetAsync<List<TaskResponseDto>>($"api/tasks/user/{userId}");

        // =========================
        // CREATE TASK ✅ SweetAlert ready
        // =========================
        public async Task<OperationResultDto?> CreateAsync(CreateTaskDto dto)
        {
            return await _api.PostAsync<CreateTaskDto, OperationResultDto>(
                "api/tasks", dto);
        }

        // =========================
        // UPDATE TASK (optional but consistent)
        // =========================
        public async Task<OperationResultDto?> UpdateAsync(Guid id, UpdateTaskDto dto)
        {
            return await _api.PutAsync<UpdateTaskDto, OperationResultDto>(
                $"api/tasks/{id}", dto);
        }

        // =========================
        // DELETE TASK (status-based)
        // =========================
        public async Task DeleteAsync(Guid id)
        {
            await _api.DeleteAsync($"api/tasks/{id}");
        }

        // =========================
        // DASHBOARD HELPERS (UNCHANGED)
        // =========================
        public async Task<int> GetTotalCountAsync()
            => (await _api.GetAsync<int?>("api/tasks/count")) ?? 0;

        public async Task<int> GetOpenCountAsync()
            => (await _api.GetAsync<int?>("api/tasks/open/count")) ?? 0;

        public async Task<List<TaskResponseDto>?> GetRecentTasksAsync(int take = 50)
            => await _api.GetAsync<List<TaskResponseDto>>($"api/tasks/recent?take={take}");

        public async Task<List<TaskResponseDto>?> GetRecentForChartAsync(int take = 100)
            => await _api.GetAsync<List<TaskResponseDto>>($"api/tasks/recent?take={take}");

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

            var url = "api/Tasks/paged";
            if (q.Count > 0)
                url += "?" + string.Join("&", q);

            return await _api.GetAsync<PagedResult<TaskResponseDto>>(url);
        }
    }
}
