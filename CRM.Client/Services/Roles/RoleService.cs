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

        // ✅ CREATE ROLE
        public async Task CreateRoleAsync(CreateRoleDto dto)
        {
            await _api.PostAsync<CreateRoleDto, object>("api/role/create", dto);
        }

        // ✅ ASSIGN ROLE
        public async Task AssignRoleAsync(AssignRoleDto dto)
        {
            await _api.PostAsync<AssignRoleDto, object>("api/role/assign", dto);
        }
    }
}
