using CRM.Server.DTOs.Auth;
using CRM.Server.DTOs.Users;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Text.Encodings.Web;
using System.Text.Json;
using CRM.Server.Security;
using Microsoft.Extensions.Options;

namespace CRM.Server.Services
{
    public class UserService : IUserService
    {
        // ---------- Dependencies ----------
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly IAuditLogService _auditLogService;
        private readonly IRoleRepository _roleRepository;

        // NEW: password policy options
        private readonly PasswordPolicySettings _passwordPolicySettings;

        public UserService(
            IUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration config,
            IAuditLogService auditLogService,
            IRoleRepository roleRepository,
            IOptions<PasswordPolicySettings> passwordPolicyOptions // NEW
        )
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _emailService = emailService;
            _config = config;
            _auditLogService = auditLogService;
            _roleRepository = roleRepository;

            // NEW
            _passwordPolicySettings = passwordPolicyOptions?.Value ?? new PasswordPolicySettings();
        }


        // NEW: Check if password is expired
        public Task<bool> IsPasswordExpiredAsync(ApplicationUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            // If PasswordLastChanged is default/min value, treat it as created at time
            var lastChanged = user.PasswordLastChanged == default
                ? user.CreatedAt
                : user.PasswordLastChanged;

            var expiryMinutes = _passwordPolicySettings?.PasswordExpiryMinutes ?? 0;

            if (expiryMinutes <= 0)
            {
                // 0 or negative => expiry disabled
                return Task.FromResult(false);
            }

            var expired = DateTime.UtcNow - lastChanged > TimeSpan.FromMinutes(expiryMinutes);
            return Task.FromResult(expired);
        }


        // Helper DTO builder (uses FRAMEWORK UserManager to get roles)
        private async Task<UserResponseDto> MapToUserDto(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            return new UserResponseDto
            {
                Id = user.Id,
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                Role = roles.FirstOrDefault() ?? "",
                IsActive = user.IsActive,
                TwoFactorEnabled = user.TwoFactorEnabled
            };
        }

        // ============================================================
        // Assign Role (Audit DTO Before + After)
        // ============================================================
        public async Task AssignRoleAsync(string userId, string roleName, string performedByUserId)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new Exception("Role name cannot be empty.");

            var user = await _userManager.FindByIdAsync(userId)
                       ?? throw new Exception("User not found");

            var role = await _roleRepository.GetByNameAsync(roleName)
                       ?? throw new Exception("Role does not exist.");

            // Old value (refactored to avoid nested await in expressions)
            var oldDto = await MapToUserDto(user);
            var oldValue = JsonSerializer.Serialize(oldDto);

            var currentRoles = (await _userManager.GetRolesAsync(user)).ToList();

            if (!currentRoles.Contains(roleName))
            {
                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                await _userManager.AddToRoleAsync(user, roleName);
            }

            // New value
            var updatedUser = await _userManager.FindByIdAsync(userId);
            var newDto = await MapToUserDto(updatedUser);
            var newValue = JsonSerializer.Serialize(newDto);

            await SafeAudit(performedByUserId, userId, "Role Assigned", "User", oldValue, newValue);
        }

        // ============================================================
        // Create User (Admin)
        // ============================================================
        public async Task CreateUserAsync(CreateUserDto dto, string performedByUserId)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null) throw new Exception("Email already exists");

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

            var newDto = await MapToUserDto(user);
            var newValue = JsonSerializer.Serialize(newDto);

            await SafeAudit(performedByUserId, user.Id, "User Created", "User", null, newValue);
        }

        // ============================================================
        // Get All Users
        // ============================================================
        public async Task<List<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var result = new List<UserResponseDto>();

            foreach (var user in users)
                result.Add(await MapToUserDto(user));

            return result;
        }

        // ============================================================
        // Invite User
        // ============================================================
        public async Task InviteUserAsync(InviteUserDto dto, string performedByUserId)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null) throw new Exception("Email already exists");

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

            var creation = await _userManager.CreateAsync(user);
            if (!creation.Succeeded)
                throw new Exception(creation.Errors.First().Description);

            await _userManager.AddToRoleAsync(user, dto.Role);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var inviteLink =
                $"{_config["Client:Url"]}/complete-registration?token={Uri.EscapeDataString(token)}&email={user.Email}";

            await _emailService.SendAsync(
                user.Email!,
                "CRM Registration Invite",
                $"Click the link to complete registration:\n\n{inviteLink}"
            );

            var newDto = await MapToUserDto(user);
            var newValue = JsonSerializer.Serialize(newDto);

            await SafeAudit(performedByUserId, user.Id, "User Invited", "User", null, newValue);
        }



        public async Task CompleteRegistrationAsync(CompleteRegistrationDto dto)
        {
            // 1. Load user
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsInvitePending)
                throw new Exception("Invalid or already completed invite");

            // 2. Check invite expiry
            if (user.InviteExpiry.HasValue && user.InviteExpiry < DateTime.UtcNow)
                throw new Exception("Invitation link has expired");

            // 3. Decode token (MUST match InviteUserAsync)
            var decodedToken = Uri.UnescapeDataString(dto.Token);

            // 4. Reset password
            var result = await _userManager.ResetPasswordAsync(
                user,
                decodedToken,
                dto.Password);

            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);

            // 5. Activate account
            var oldDto = await MapToUserDto(user);
            var oldValue = JsonSerializer.Serialize(oldDto);

            user.IsInvitePending = false;
            user.IsActive = true;
            user.InviteExpiry = null;
            user.PasswordLastChanged = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            // 6. Audit
            var newDto = await MapToUserDto(user);
            var newValue = JsonSerializer.Serialize(newDto);

            await SafeAudit(
                user.Id,                 // performedByUserId (self)
                user.Id,                 // targetUserId
                "User Registration Completed",
                "User",
                oldValue,
                newValue
            );
        }



        // ============================================================
        // Deactivate User
        // ============================================================
        public async Task DeactivateUserAsync(string userId, string performedByUserId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                       ?? throw new Exception("User not found");

            var oldDto = await MapToUserDto(user);
            var oldValue = JsonSerializer.Serialize(oldDto);

            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            var newDto = await MapToUserDto(user);
            var newValue = JsonSerializer.Serialize(newDto);

            await SafeAudit(performedByUserId, userId, "User Deactivated", "User", oldValue, newValue);
        }

        // ============================================================
        // Activate User
        // ============================================================
        public async Task ActivateUserAsync(string userId, string performedByUserId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                       ?? throw new Exception("User not found");

            var oldDto = await MapToUserDto(user);
            var oldValue = JsonSerializer.Serialize(oldDto);

            user.IsActive = true;
            await _userRepository.UpdateAsync(user);

            var updated = await _userManager.FindByIdAsync(userId);
            var newDto = await MapToUserDto(updated);
            var newValue = JsonSerializer.Serialize(newDto);

            await SafeAudit(performedByUserId, userId, "User Activated", "User", oldValue, newValue);
        }

        // ============================================================
        // Update User
        // ============================================================
        public async Task UpdateUserAsync(string userId, UpdateUserDto dto, string performedByUserId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                       ?? throw new Exception("User not found");

            var oldDto = await MapToUserDto(user);
            var oldValue = JsonSerializer.Serialize(oldDto);

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

            var newDto = await MapToUserDto(user);
            var newValue = JsonSerializer.Serialize(newDto);

            await SafeAudit(performedByUserId, userId, "User Updated", "User", oldValue, newValue);
        }

        // ============================================================
        // Delete User
        // ============================================================
        public async Task DeleteUserAsync(string userId, string performedByUserId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                       ?? throw new Exception("User not found");

            var oldDto = await MapToUserDto(user);
            var oldValue = JsonSerializer.Serialize(oldDto);

            await _userRepository.DeleteAsync(user);

            await SafeAudit(performedByUserId, userId, "User Deleted", "User", oldValue, null);
        }

        // ============================================================
        // Get by ID
        // ============================================================
        public async Task<UserResponseDto> GetUserByIdAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                       ?? throw new Exception("User not found");

            return await MapToUserDto(user);
        }

        // ============================================================
        // Filter users
        // ============================================================
        public async Task<List<UserResponseDto>> FilterUsersAsync(string? role, bool? isActive)
        {
            var users = _userManager.Users.ToList();
            var result = new List<UserResponseDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if ((string.IsNullOrEmpty(role) || roles.Contains(role)) &&
                    (!isActive.HasValue || user.IsActive == isActive.Value))
                {
                    result.Add(await MapToUserDto(user));
                }
            }

            return result;
        }

        // ============================================================
        // MFA (no audit)
        // ============================================================
        public async Task<EnableMfaResponseDto> EnableMfaAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                       ?? throw new Exception("User not found");

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
        // 9) Reset Password (no audit here - handled in auth)
        // ============================================================
        public async Task ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                throw new Exception("Invalid reset request");

            var decodedToken = Uri.UnescapeDataString(token);

            // Reset password without knowing the old password
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);

            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);

            // Update password timestamp for security policies (same as ChangePassword)
            user.PasswordLastChanged = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // OPTIONAL: revoke refresh tokens (recommended for security)
            // await _refreshTokenService.RevokeAllRefreshTokensForUserAsync(user.Id);
        }


        // ============================================================
        // 10) Change Password (no audit here - handled in auth)
        // ============================================================
        // in UserService.cs
        public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);

            // Update password timestamp
            user.PasswordLastChanged = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // OPTIONAL: revoke refresh tokens / sessions here via your JWT service or repository
            // If you have a method like _jwtTokenService.RevokeAllTokensForUser(user.Id) - call it here.
            // e.g. await _jwtTokenService.RevokeAllRefreshTokensAsync(user.Id);

            // Note: AuthController already records audit after calling this service.
        }



        public async Task ChangePasswordByEmailAsync(string email, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new Exception("Invalid request");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                throw new Exception("Invalid request");

            // Verify old password (knowledge proof)
            var isValid = await _userManager.CheckPasswordAsync(user, currentPassword);
            if (!isValid)
                throw new Exception("Invalid credentials");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);

            // Update password timestamp
            user.PasswordLastChanged = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // OPTIONAL: revoke refresh tokens / sessions via JWT service (implement later)
            // e.g. await _jwtTokenService.RevokeAllRefreshTokensAsync(user.Id);

            // NOTE: AuthController logs audit after calling service (or you can call SafeAudit here if you prefer)
        }

        // ============================================================
        // SAFE AUDIT
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
                // DO NOT BREAK BUSINESS LOGIC
            }
        }
    }
}


