using CRM.Server.DTOs.Users;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace CRM.Server.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        // ✅ Built-in: UserManager, IConfiguration
        // ✅ Custom: IUserRepository, IEmailService
        public UserService(
            IUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration config)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _emailService = emailService;
            _config = config;
        }

        // ============================================================
        // ✅ 1️⃣ MANUAL ADMIN USER CREATION (TEMP PASSWORD)
        // ============================================================
        public async Task CreateUserAsync(CreateUserDto dto)
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

            // ✅ Strong temporary password
            string tempPassword = Guid.NewGuid().ToString("N")[..8] + "@A1";

            var result = await _userManager.CreateAsync(user, tempPassword);
            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);

            // ✅ Assign role
            var roleResult = await _userManager.AddToRoleAsync(user, dto.Role);
            if (!roleResult.Succeeded)
                throw new Exception("Role assignment failed");

            // ✅ Optional: Send temp password via email
            await _emailService.SendAsync(
                user.Email!,
                "Your CRM Account Credentials",
                $"Your temporary password is: {tempPassword}"
            );
        }

        // ============================================================
        // ✅ 2️⃣ GET ALL USERS (ADMIN VIEW)
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
                    Role = roles.FirstOrDefault() ?? ""
                });
            }

            return result;
        }

        // ============================================================
        // ✅ 3️⃣ EMAIL INVITE USER (REGISTRATION LINK FLOW)
        // ============================================================
        public async Task InviteUserAsync(InviteUserDto dto)
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

            // ✅ Generate secure registration token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var inviteLink =
                $"{_config["Client:Url"]}/register?token={Uri.EscapeDataString(token)}&email={user.Email}";

            //var inviteLink =
            //$"https://localhost:7194/api/auth/complete-registration?token={Uri.EscapeDataString(token)}&email={user.Email}";



            // ✅ Send registration email
            await _emailService.SendAsync(
                user.Email!,
                "CRM Registration Invite",
                $"Click the link to complete your registration:\n\n{inviteLink}"
            );
        }

        // ============================================================
        // ✅ 4️⃣ DEACTIVATE USER (ADMIN CONTROL)
        // ============================================================
        public async Task DeactivateUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            user.IsActive = false;
            await _userManager.UpdateAsync(user);
        }


        public async Task ActivateUserAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                throw new Exception("User not found");

            user.IsActive = true;

            await _userRepository.UpdateAsync(user);
        }



        public async Task UpdateUserAsync(string userId, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                throw new Exception("User not found");

            user.FullName = dto.FullName;
            user.Email = dto.Email;

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                // Using roleManager or your RoleService logic
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                await _userManager.AddToRoleAsync(user, dto.Role);
            }

            if (dto.IsActive.HasValue)
                user.IsActive = dto.IsActive.Value;

            await _userRepository.UpdateAsync(user);
        }

        public async Task DeleteUserAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                throw new Exception("User not found");

            await _userRepository.DeleteAsync(user);
        }


        // ============================================================
        // ✅ 5️⃣ FILTER USERS (BY ROLE & ACTIVE STATUS)
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
                        Role = roles.FirstOrDefault() ?? ""
                    });
                }
            }

            return result;
        }
    }
}
