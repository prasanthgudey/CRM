using CRM.Server.DTOs.Auth;
using CRM.Server.DTOs.Users;

namespace CRM.Server.Services.Interfaces
{
    public interface IUserService
    {
        Task CreateUserAsync(CreateUserDto dto, string performedByUserId);
        Task<List<UserResponseDto>> GetAllUsersAsync();

        Task InviteUserAsync(InviteUserDto dto, string performedByUserId);
        Task DeactivateUserAsync(string userId, string performedByUserId);
        Task ActivateUserAsync(string userId, string performedByUserId);

        Task UpdateUserAsync(string userId, UpdateUserDto dto, string performedByUserId);
        Task DeleteUserAsync(string userId, string performedByUserId);

        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(string email, string token, string newPassword);
        Task ChangePasswordAsync(string userId, string currentPassword, string newPassword);

        Task<UserResponseDto> GetUserByIdAsync(string userId);
        Task<List<UserResponseDto>> FilterUsersAsync(string? role, bool? isActive);

        Task<EnableMfaResponseDto> EnableMfaAsync(string userId);

        Task VerifyMfaAsync(string userId, string code);
        Task DisableMfaAsync(string userId, string code);

        Task AssignRoleAsync(string userId, string roleName, string performedByUserId);
    }
}
