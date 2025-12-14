using CRM.Server.DTOs.Auth;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CRM.Server.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, HttpContext context);
        Task<AuthResponseDto> MfaLoginAsync(MfaLoginDto dto, HttpContext context); // ✅ ADD
        Task<AuthResponseDto> RefreshAsync(RefreshRequestDto dto, HttpContext context);
        Task LogoutAsync(ClaimsPrincipal user, HttpContext context);
    }
}
