using CRM.Client.DTOs.Roles;
using CRM.Client.Services; // make sure this matches where ServiceResult lives
using CRM.Client.Services.Http;
using System.Net;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace CRM.Client.Services.Roles
{
    public class RoleService
    {
        private readonly ApiClientService _api;

        public RoleService(ApiClientService api)
        {
            _api = api;
        }

        // CREATE ROLE
        public async Task CreateRoleAsync(CreateRoleDto dto)
        {
            await _api.PostAsync("api/role/create", dto);
        }

        // GET ALL ROLES
        public async Task<List<RoleResponseDto>?> GetAllRolesAsync()

        {
            return await _api.GetAsync<List<RoleResponseDto>>("api/role");
        }

        // GET ROLE BY NAME
        public async Task<RoleResponseDto?> GetRoleAsync(string roleName)
        {
            return await _api.GetAsync<RoleResponseDto>($"api/role/{roleName}");
        }

        //   UPDATE / RENAME ROLE
        //public async Task UpdateRoleAsync(UpdateRoleDto dto)
        //{
        //    await _api.PutAsync<UpdateRoleDto, object?>(
        //        "api/role/update",
        //        dto
        //    );
        //}
        public async Task<ServiceResult> UpdateRoleAsync(UpdateRoleDto dto)
        {
            try
            {
                var response = await _api.PutRawAsync("api/role/update", dto);

                var body = await response.Content.ReadAsStringAsync();

                // 409 => duplicate role
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,
                        StatusCode = 409,
                        ErrorMessage = "Role already exists"
                    };
                }

                // SUCCESS CASE
                if (response.IsSuccessStatusCode)
                {
                    return new ServiceResult
                    {
                        IsSuccess = true,
                        StatusCode = (int)response.StatusCode
                    };
                }

                // ANY OTHER ERROR
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



        // DELETE ROLE
        public async Task DeleteRoleAsync(string roleName)
        {
            await _api.DeleteAsync($"api/role/{roleName}");
        }

        // ASSIGN ROLE TO USER
        public async Task AssignRoleAsync(AssignRoleDto dto)
        {
            await _api.PostAsync("api/role/assign", dto);
        }
    }
}
