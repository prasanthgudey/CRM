using CRM.Server.DTOs.Users;

namespace CRM.Server.Services.Interfaces
{
    public interface IUserService
    {
        Task CreateUserAsync(CreateUserDto dto);
        Task<List<UserResponseDto>> GetAllUsersAsync();
        Task InviteUserAsync(InviteUserDto dto);
        Task DeactivateUserAsync(string userId);
        Task ActivateUserAsync(string userId);

        Task UpdateUserAsync(string userId, UpdateUserDto dto);

        Task DeleteUserAsync(string userId);

        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(string email, string token, string newPassword);
        Task ChangePasswordAsync(string userId, string currentPassword, string newPassword);

        Task<UserResponseDto> GetUserByIdAsync(string userId);

        Task<List<UserResponseDto>> FilterUsersAsync(string? role, bool? isActive);

    }
}
