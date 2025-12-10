using CRM.Client.DTOs.Roles;
using CRM.Client.Services.Http;

namespace CRM.Client.Services.Roles
{
    public class RoleService
    {
        private readonly ApiClientService _api;

        public RoleService(ApiClientService api)
        {
            _api = api;
        }

        // ============================================
        // CREATE ROLE
        // ============================================
        public async Task CreateRoleAsync(CreateRoleDto dto)
        {
            await _api.PostAsync("api/role/create", dto);
        }

        // ============================================
        // GET ALL ROLES
        // ============================================
        public async Task<List<RoleResponseDto>?> GetAllRolesAsync()
        {
            return await _api.GetAsync<List<RoleResponseDto>>("api/role");
        }

        // ============================================
        // GET ROLE BY NAME
        // ============================================
        public async Task<RoleResponseDto?> GetRoleAsync(string roleName)
        {
            return await _api.GetAsync<RoleResponseDto>($"api/role/{roleName}");
        }

        // ============================================
        // UPDATE / RENAME ROLE
        // ============================================
        public async Task UpdateRoleAsync(UpdateRoleDto dto)
        {
            await _api.PutAsync<UpdateRoleDto, object?>(
                "api/role/update",
                dto
            );
        }

        // ============================================
        // DELETE ROLE
        // ============================================
        public async Task DeleteRoleAsync(string roleName)
        {
            await _api.DeleteAsync($"api/role/{roleName}");
        }

        // ============================================
        // ASSIGN ROLE TO USER
        // ============================================
        public async Task AssignRoleAsync(AssignRoleDto dto)
        {
            await _api.PostAsync("api/role/assign", dto);
        }
    }
}
