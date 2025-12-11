using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRM.Client.DTOs.Tasks;
using CRM.Client.Services.Http;

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
    }
}
