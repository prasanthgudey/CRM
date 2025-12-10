using CRM.Client.DTOs.Users;
using CRM.Client.Services.Http;

namespace CRM.Client.Services.Users
{
    public class UserService
    {
        private readonly ApiClientService _api;

        public UserService(ApiClientService api)
        {
            _api = api;
        }

        // ✅ GET ALL USERS
        public async Task<List<UserResponseDto>?> GetAllUsersAsync()
        {
            return await _api.GetAsync<List<UserResponseDto>>("api/user");
        }

        // ✅ CREATE USER
        public async Task CreateUserAsync(CreateUserDto dto)
        {
            await _api.PostAsync<CreateUserDto, object>("api/user/create", dto);
        }

        // ✅ INVITE USER
        public async Task InviteUserAsync(InviteUserDto dto)
        {
            await _api.PostAsync<InviteUserDto, object>("api/user/invite", dto);
        }

        // ✅ DEACTIVATE USER
        public async Task DeactivateUserAsync(string userId)
        {
            await _api.PutAsync<object, object>($"api/user/deactivate/{userId}", new { });
        }

        public async Task ActivateUserAsync(string userId)
        {
            await _api.PutAsync<object, object>($"api/user/activate/{userId}", new { });
        }
        // ✅ FILTER USERS
        public async Task<List<UserResponseDto>?> FilterUsersAsync(string? role, bool? isActive)
        {
            var url = $"api/user/filter?role={role}&isActive={isActive}";
            return await _api.GetAsync<List<UserResponseDto>>(url);
        }

        // PUT /api/user/update/{userId}
        public async Task UpdateUserAsync(string userId, UpdateUserDto dto)
        {
            await _api.PutAsync<UpdateUserDto, object?>($"api/user/update/{userId}", dto);
        }

        // DELETE /api/user/{userId}
        public async Task DeleteUserAsync(string userId)
        {
            await _api.DeleteAsync($"api/user/{userId}");
        }

    }
}
