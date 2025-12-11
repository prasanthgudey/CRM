using CRM.Server.DTOs.Users;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using CRM.Server.DTOs.Auth;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CRM.Server.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly IAuditLogService _auditLogService;

        public UserService(
            IUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration config,
            IAuditLogService auditLogService)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _emailService = emailService;
            _config = config;
            _auditLogService = auditLogService;
        }

        // ============================================================
        // ✅ 1️⃣ MANUAL ADMIN USER CREATION (WITH AUDIT)
        // ============================================================
        public async Task CreateUserAsync(CreateUserDto dto, string performedByUserId)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                throw new Exception("Email already exists");

            var user = new ApplicationUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            string tempPassword = Guid.NewGuid().ToString("N")[..8] + "@A1";

            var result = await _userManager.CreateAsync(user, tempPassword);
            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);

            await _userManager.AddToRoleAsync(user, dto.Role);

            await _emailService.SendAsync(
                user.Email!,
                "Your CRM Account Credentials",
                $"Your temporary password is: {tempPassword}"
            );

            await SafeAudit(
                performedByUserId,
                user.Id,
                "User Created",
                "User",
                null,
                JsonSerializer.Serialize(user)
            );
        }

        // ============================================================
        // ✅ 2️⃣ GET ALL USERS (NO AUDIT REQUIRED)
        // ============================================================
        public async Task<List<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var result = new List<UserResponseDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new UserResponseDto
                {
                    Id = user.Id,
                    FullName = user.FullName ?? "",
                    Email = user.Email ?? "",
                    IsActive = user.IsActive,
                    Role = roles.FirstOrDefault() ?? "",
                    TwoFactorEnabled = user.TwoFactorEnabled
                });
            }

            return result;
        }

        // ============================================================
        // ✅ 3️⃣ EMAIL INVITE USER (WITH AUDIT)
        // ============================================================
        public async Task InviteUserAsync(InviteUserDto dto, string performedByUserId)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                throw new Exception("Email already exists");

            var user = new ApplicationUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email,
                IsActive = false,
                IsInvitePending = true,
                InviteExpiry = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);

            await _userManager.AddToRoleAsync(user, dto.Role);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var inviteLink =
                $"{_config["Client:Url"]}/register?token={Uri.EscapeDataString(token)}&email={user.Email}";

            await _emailService.SendAsync(
                user.Email!,
                "CRM Registration Invite",
                $"Click the link to complete your registration:\n\n{inviteLink}"
            );

            await SafeAudit(
                performedByUserId,
                user.Id,
                "User Invited",
                "User",
                null,
                JsonSerializer.Serialize(new { user.Email, Role = dto.Role })
            );
        }

        // ============================================================
        // ✅ 4️⃣ DEACTIVATE USER (WITH AUDIT)
        // ============================================================
        public async Task DeactivateUserAsync(string userId, string performedByUserId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var oldValue = JsonSerializer.Serialize(new { user.IsActive });

            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            var newValue = JsonSerializer.Serialize(new { user.IsActive });

            await SafeAudit(
                performedByUserId,
                userId,
                "User Deactivated",
                "User",
                oldValue,
                newValue
            );
        }

        // ============================================================
        // ✅ 5️⃣ ACTIVATE USER (WITH AUDIT)
        // ============================================================
        public async Task ActivateUserAsync(string userId, string performedByUserId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var oldValue = JsonSerializer.Serialize(new { user.IsActive });

            user.IsActive = true;
            await _userRepository.UpdateAsync(user);

            var newValue = JsonSerializer.Serialize(new { user.IsActive });

            await SafeAudit(
                performedByUserId,
                userId,
                "User Activated",
                "User",
                oldValue,
                newValue
            );
        }

        // ============================================================
        // ✅ 6️⃣ UPDATE USER (WITH AUDIT)
        // ============================================================
        public async Task UpdateUserAsync(string userId, UpdateUserDto dto, string performedByUserId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var oldValue = JsonSerializer.Serialize(user);

            user.FullName = dto.FullName;
            user.Email = dto.Email;

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                await _userManager.AddToRoleAsync(user, dto.Role);
            }

            if (dto.IsActive.HasValue)
                user.IsActive = dto.IsActive.Value;

            await _userRepository.UpdateAsync(user);

            var newValue = JsonSerializer.Serialize(user);

            await SafeAudit(
                performedByUserId,
                userId,
                "User Updated",
                "User",
                oldValue,
                newValue
            );
        }

        // ============================================================
        // ✅ 7️⃣ DELETE USER (WITH AUDIT)
        // ============================================================
        public async Task DeleteUserAsync(string userId, string performedByUserId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var oldValue = JsonSerializer.Serialize(user);

            await _userRepository.DeleteAsync(user);

            await SafeAudit(
                performedByUserId,
                userId,
                "User Deleted",
                "User",
                oldValue,
                null
            );
        }

        // ============================================================
        // ✅ 8️⃣ FORGOT PASSWORD (NO AUDIT HERE - DONE IN AUTH)
        // ============================================================
        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink =
                $"{_config["Client:Url"]}/reset-password?token={Uri.EscapeDataString(token)}&email={user.Email}";

            await _emailService.SendAsync(
                user.Email!,
                "CRM Password Reset",
                $"Click the link below to reset your password:\n\n{resetLink}"
            );
        }

        // ============================================================
        // ✅ 9️⃣ RESET PASSWORD (NO AUDIT HERE - DONE IN AUTH)
        // ============================================================
        public async Task ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                throw new Exception("Invalid reset request");

            var decodedToken = Uri.UnescapeDataString(token);

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);

            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);
        }

        // ============================================================
        // ✅ 🔟 CHANGE PASSWORD (NO AUDIT HERE - DONE IN AUTH)
        // ============================================================
        public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var result = await _userManager.ChangePasswordAsync(
                user, currentPassword, newPassword);

            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);
        }

        // ============================================================
        // ✅ GET USER BY ID
        // ============================================================
        public async Task<UserResponseDto> GetUserByIdAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var roles = await _userManager.GetRolesAsync(user);

            return new UserResponseDto
            {
                Id = user.Id,
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                IsActive = user.IsActive,
                Role = roles.FirstOrDefault() ?? "",
                TwoFactorEnabled = user.TwoFactorEnabled
            };
        }

        // ============================================================
        // ✅ FILTER USERS
        // ============================================================
        public async Task<List<UserResponseDto>> FilterUsersAsync(string? role, bool? isActive)
        {
            var users = _userManager.Users.AsQueryable();

            if (isActive.HasValue)
                users = users.Where(x => x.IsActive == isActive.Value);

            var userList = users.ToList();
            var result = new List<UserResponseDto>();

            foreach (var user in userList)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (string.IsNullOrEmpty(role) || roles.Contains(role))
                {
                    result.Add(new UserResponseDto
                    {
                        Id = user.Id,
                        FullName = user.FullName ?? "",
                        Email = user.Email ?? "",
                        IsActive = user.IsActive,
                        Role = roles.FirstOrDefault() ?? "",
                        TwoFactorEnabled = user.TwoFactorEnabled
                    });
                }
            }

            return result;
        }

        // ============================================================
        // ✅ MFA METHODS (NO AUDIT HERE - DONE IN AUTH)
        // ============================================================
        public async Task<EnableMfaResponseDto> EnableMfaAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            await _userManager.ResetAuthenticatorKeyAsync(user);

            var key = await _userManager.GetAuthenticatorKeyAsync(user);

            var email = user.Email!;
            var issuer = "CRM";

            var qrCodeUri =
                $"otpauth://totp/{UrlEncoder.Default.Encode(issuer)}:" +
                $"{UrlEncoder.Default.Encode(email)}" +
                $"?secret={key}&issuer={UrlEncoder.Default.Encode(issuer)}&digits=6";

            return new EnableMfaResponseDto
            {
                SharedKey = key!,
                QrCodeImageUrl = qrCodeUri
            };
        }

        public async Task VerifyMfaAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                code);

            if (!isValid)
                throw new Exception("Invalid verification code");

            user.TwoFactorEnabled = true;
            await _userManager.UpdateAsync(user);
        }

        public async Task DisableMfaAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                code);

            if (!isValid)
                throw new Exception("Invalid verification code");

            user.TwoFactorEnabled = false;
            await _userManager.UpdateAsync(user);

            await _userManager.ResetAuthenticatorKeyAsync(user);
        }

        // ============================================================
        // ✅ SAFE AUDIT HELPER
        // ============================================================
        private async Task SafeAudit(
            string performedByUserId,
            string targetUserId,
            string action,
            string entityName,
            string? oldValue,
            string? newValue)
        {
            try
            {
                await _auditLogService.LogAsync(
                    performedByUserId,
                    targetUserId,
                    action,
                    entityName,
                    true,
                    null,
                    oldValue,
                    newValue
                );
            }
            catch
            {
                // ✅ Never break business logic
            }
        }
    }
}
