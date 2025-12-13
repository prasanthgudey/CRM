using CRM.Client.DTOs.Roles;
using CRM.Client.Services;
using CRM.Client.Services.Http;
using System.Net;

namespace CRM.Client.Services.Roles
{
    public class RoleService
    {
        private readonly ApiClientService _api;

        public RoleService(ApiClientService api)
        {
            _api = api;
        }

        // ============================
        // CREATE ROLE  ✅ FIXED
        // ============================
        public async Task CreateRoleAsync(CreateRoleDto dto)
        {
            // IMPORTANT:
            // Do NOT parse response body on success
            // API returns plain text, not JSON

            var response = await _api.PostRawAsync("api/role/create", dto);

            if (response.IsSuccessStatusCode)
                return;

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        // ============================
        // GET ALL ROLES
        // ============================
        public async Task<List<RoleResponseDto>?> GetAllRolesAsync()
        {
            return await _api.GetAsync<List<RoleResponseDto>>("api/role");
        }

        // ============================
        // GET ROLE BY NAME
        // ============================
        public async Task<RoleResponseDto?> GetRoleAsync(string roleName)
        {
            return await _api.GetAsync<RoleResponseDto>($"api/role/{roleName}");
        }

        // ============================
        // UPDATE / RENAME ROLE
        // ============================
        public async Task<ServiceResult> UpdateRoleAsync(UpdateRoleDto dto)
        {
            try
            {
                var response = await _api.PutRawAsync("api/role/update", dto);
                var body = await response.Content.ReadAsStringAsync();

                // DUPLICATE ROLE
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,
                        StatusCode = 409,
                        ErrorMessage = "Role already exists"
                    };
                }

                // SUCCESS
                if (response.IsSuccessStatusCode)
                {
                    return new ServiceResult
                    {
                        IsSuccess = true,
                        StatusCode = (int)response.StatusCode
                    };
                }

                // OTHER ERRORS
                return new ServiceResult
                {
                    IsSuccess = false,
                    StatusCode = (int)response.StatusCode,
                    ErrorMessage = body
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ============================
        // DELETE ROLE
        public async Task DeleteRoleAsync(string roleName)
        {
            // ApiClientService.DeleteAsync already:
            // - throws exception on failure
            // - returns nothing on success

            await _api.DeleteAsync($"api/role/{roleName}");
        }


        // ============================
        // ASSIGN ROLE TO USER
        // ============================
        public async Task AssignRoleAsync(AssignRoleDto dto)
        {
            var response = await _api.PostRawAsync("api/user/assign", dto);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }
        }
    }
}
