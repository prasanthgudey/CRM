using CRM.Client.DTOs.Users;
using CRM.Client.DTOs.Auth;
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

        // ✅ GET MY PROFILE (logged-in user)
        public async Task<UserResponseDto?> GetMyProfileAsync()
        {
            return await _api.GetAsync<UserResponseDto>("api/user/me");
        }

        // ✅ GET USER BY ID (admin / detail)
        public async Task<UserResponseDto?> GetUserByIdAsync(string userId)
        {
            return await _api.GetAsync<UserResponseDto>($"api/user/{userId}");
        }

        // ✅ GET ALL USERS
        public async Task<List<UserResponseDto>?> GetAllUsersAsync()
        {
            return await _api.GetAsync<List<UserResponseDto>>("api/user");
        }

        // ✅ CREATE USER
        //public async Task CreateUserAsync(CreateUserDto dto)
        //{
        //    await _api.PostAsync<CreateUserDto, object>("api/user/create", dto);
        //}
        // ⭐ UPDATED METHOD - now returns OperationResultDto instead of void
        public async Task<OperationResultDto?> CreateUserAsync(CreateUserDto dto)
        {
            return await _api.PostAsync<CreateUserDto, OperationResultDto>("api/user/create", dto);  // ⭐ new return type
        }


        // ✅ INVITE USER
        public async Task<OperationResultDto?> InviteUserAsync(InviteUserDto dto)   // ⭐ new code
        {
            return await _api.PostAsync<InviteUserDto, OperationResultDto>(
                "api/account/invite", dto);                                            // ⭐ new code
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
