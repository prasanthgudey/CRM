using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Server.DTOs.Users;
using CRM.Server.DTOs.Auth;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CRM.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IEmailService> _emailMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<IAuditLogService> _auditMock;
        private readonly Mock<IRoleRepository> _roleRepoMock;

        private readonly UserService _service;

        public UserServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _userManagerMock = MockUserManager();
            _emailMock = new Mock<IEmailService>();
            _configMock = new Mock<IConfiguration>();
            _auditMock = new Mock<IAuditLogService>();
            _roleRepoMock = new Mock<IRoleRepository>();

            _configMock.Setup(c => c["Client:Url"]).Returns("https://app.test");

            _service = new UserService(
                _userRepoMock.Object,
                _userManagerMock.Object,
                _emailMock.Object,
                _configMock.Object,
                _auditMock.Object,
                _roleRepoMock.Object
            );
        }

        private Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );
        }

        private ApplicationUser User(string id = "u1", string email = "a@b.com") =>
            new ApplicationUser { Id = id, Email = email, UserName = email, FullName = "Test", IsActive = true };

        // ---------------------------------------------------------
        // 1) Assign Role — Main Business Logic
        // ---------------------------------------------------------
        [Fact]
        public async Task AssignRoleAsync_Assigns_And_Audits()
        {
            var user = User("u1");
            _userManagerMock.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(user);
            _roleRepoMock.Setup(r => r.GetByNameAsync("Admin"))
                .ReturnsAsync(new IdentityRole("Admin"));

            _userManagerMock.Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "OldRole" });

            _userManagerMock.Setup(u => u.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(u => u.AddToRoleAsync(user, "Admin"))
                .ReturnsAsync(IdentityResult.Success);

            await _service.AssignRoleAsync("u1", "Admin", "actor");

            _auditMock.Verify(a => a.LogAsync(
                "actor",
                "u1",
                "Role Assigned",
                "User",
                true,
                null,
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Admin"))
            ), Times.Once);
        }

        // ---------------------------------------------------------
        // 2) Create User — Creates, Sends Email, Audits
        // ---------------------------------------------------------
        [Fact]
        public async Task CreateUserAsync_Creates_SendsEmail_And_Audits()
        {
            var dto = new CreateUserDto
            {
                FullName = "Test User",
                Email = "new@t.com",
                Role = "Admin"
            };

            _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
                .ReturnsAsync(IdentityResult.Success);

            _emailMock.Setup(e => e.SendAsync(dto.Email, It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _service.CreateUserAsync(dto, "admin");

            _emailMock.Verify(e =>
                e.SendAsync(dto.Email, It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);

            _auditMock.Verify(a => a.LogAsync(
                "admin",
                It.IsAny<string>(),
                "User Created",
                "User",
                true,
                null,
                null,
                It.IsAny<string>()
            ));
        }

        // ---------------------------------------------------------
        // 3) Invite User — Sends Invite Email, Audits
        // ---------------------------------------------------------
        [Fact]
        public async Task InviteUserAsync_SendsInviteEmail_And_Audits()
        {
            var dto = new InviteUserDto
            {
                Email = "invite@t.com",
                FullName = "Invite User",
                Role = "User"
            };

            _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), dto.Role))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(u => u.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("token123");

            _emailMock.Setup(e => e.SendAsync(dto.Email, It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _service.InviteUserAsync(dto, "actor");

            _emailMock.Verify(e =>
                e.SendAsync(dto.Email, It.IsAny<string>(), It.Is<string>(s => s.Contains("https://app.test"))),
                Times.Once);

            _auditMock.Verify(a => a.LogAsync(
                "actor",
                It.IsAny<string>(),
                "User Invited",
                "User",
                true,
                null,
                null,
                It.IsAny<string>()
            ), Times.Once);
        }

        // ---------------------------------------------------------
        // 4) Deactivate User
        // ---------------------------------------------------------
        [Fact]
        public async Task DeactivateUserAsync_Updates_And_Audits()
        {
            var user = User("u1");
            _userManagerMock.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            await _service.DeactivateUserAsync("u1", "actor");

            _auditMock.Verify(a => a.LogAsync(
                "actor",
                "u1",
                "User Deactivated",
                "User",
                true,
                null,
                It.IsAny<string>(),
                It.IsAny<string>()
            ));
        }

        // ---------------------------------------------------------
        // 5) Activate User
        // ---------------------------------------------------------
        [Fact]
        public async Task ActivateUserAsync_Updates_And_Audits()
        {
            var user = User("u1");
            _userRepoMock.Setup(r => r.GetByIdAsync("u1")).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);
            _userManagerMock.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            await _service.ActivateUserAsync("u1", "actor");

            _auditMock.Verify(a => a.LogAsync(
                "actor",
                "u1",
                "User Activated",
                "User",
                true,
                null,
                It.IsAny<string>(),
                It.IsAny<string>()
            ));
        }

        // ---------------------------------------------------------
        // 6) Update User (Role + Fields + Audit)
        // ---------------------------------------------------------
        [Fact]
        public async Task UpdateUserAsync_UpdatesUser_And_Audits()
        {
            var user = User("u1");
            _userRepoMock.Setup(r => r.GetByIdAsync("u1")).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Old" });
            _userManagerMock.Setup(u => u.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(u => u.AddToRoleAsync(user, "NewRole"))
                .ReturnsAsync(IdentityResult.Success);

            var dto = new UpdateUserDto
            {
                FullName = "Updated",
                Email = "updated@t.com",
                Role = "NewRole",
                IsActive = false
            };

            await _service.UpdateUserAsync("u1", dto, "actor");

            _auditMock.Verify(a => a.LogAsync(
                "actor",
                "u1",
                "User Updated",
                "User",
                true,
                null,
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Once);
        }

        // ---------------------------------------------------------
        // 7) Delete User
        // ---------------------------------------------------------
        [Fact]
        public async Task DeleteUserAsync_Deletes_And_Audits()
        {
            var user = User("u1");
            _userRepoMock.Setup(r => r.GetByIdAsync("u1")).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.DeleteAsync(user)).Returns(Task.CompletedTask);

            await _service.DeleteUserAsync("u1", "actor");

            _auditMock.Verify(a => a.LogAsync(
                "actor",
                "u1",
                "User Deleted",
                "User",
                true,
                null,
                It.IsAny<string>(),
                null
            ));
        }

        // ---------------------------------------------------------
        // 8) Enable MFA
        // ---------------------------------------------------------
        [Fact]
        public async Task EnableMfaAsync_Returns_QR_And_Key()
        {
            var user = User("u1", "x@y.com");
            _userManagerMock.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.ResetAuthenticatorKeyAsync(user))
     .Returns(async () => { });

            _userManagerMock.Setup(u => u.GetAuthenticatorKeyAsync(user))
                .ReturnsAsync("SECRET123");

            var res = await _service.EnableMfaAsync("u1");

            Assert.Equal("SECRET123", res.SharedKey);
            Assert.Contains("otpauth://totp", res.QrCodeImageUrl);
        }

        // ---------------------------------------------------------
        // 9) Forgot Password — Sends Email
        // ---------------------------------------------------------
        [Fact]
        public async Task ForgotPasswordAsync_SendsResetEmail()
        {
            var user = User("u1", "reset@t.com");
            _userManagerMock.Setup(u => u.FindByEmailAsync("reset@t.com"))
                .ReturnsAsync(user);

            _userManagerMock.Setup(u => u.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("reset_token");

            _emailMock.Setup(e => e.SendAsync(user.Email!, It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _service.ForgotPasswordAsync("reset@t.com");

            _emailMock.Verify(e => e.SendAsync(
                "reset@t.com",
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("https://app.test"))
            ));
        }

        // ---------------------------------------------------------
        // 10) Change Password — Success Enables Audit
        // ---------------------------------------------------------
        [Fact]
        public async Task ChangePasswordAsync_WhenFailed_Throws()
        {
            var user = User("u1");
            _userManagerMock.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(user);

            _userManagerMock.Setup(u => u.ChangePasswordAsync(user, "old", "new"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "bad" }));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.ChangePasswordAsync("u1", "old", "new"));
        }
    }
}
