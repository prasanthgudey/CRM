using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Server.DTOs.Auth;
using CRM.Server.DTOs.Users;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services;
using CRM.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CRM.Server.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _repoMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IEmailService> _emailMock;
        private readonly Mock<IConfiguration> _configMock;

        private readonly UserService _service;

        public UserServiceTests()
        {
            _repoMock = new Mock<IUserRepository>();
            _emailMock = new Mock<IEmailService>();
            _configMock = new Mock<IConfiguration>();

            // ---------------------------
            // Mock UserManager
            // ---------------------------
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );

            _service = new UserService(
                _repoMock.Object,
                _userManagerMock.Object,
                _emailMock.Object,
                _configMock.Object
            );
        }

        // ============================================================
        // CREATE USER
        // ============================================================
        [Fact]
        public async Task CreateUserAsync_Should_Create_User_When_Email_Not_Exists()
        {
            var dto = new CreateUserDto
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Role = "Admin"
            };

            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), dto.Role))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            Func<Task> act = async () => await _service.CreateUserAsync(dto);

            // Assert
            await act.Should().NotThrowAsync();
            _emailMock.Verify(e => e.SendAsync(dto.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // ============================================================
        // GET ALL USERS
        // ============================================================
        [Fact]
        public async Task GetAllUsersAsync_Should_Return_Mapped_Dto_List()
        {
            var user = new ApplicationUser { Id = "1", Email = "a@b.com", FullName = "Test User", IsActive = true };

            _repoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<ApplicationUser> { user });

            _userManagerMock.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Admin" });

            var result = await _service.GetAllUsersAsync();

            result.Should().HaveCount(1);
            result.First().Role.Should().Be("Admin");
        }

        // ============================================================
        // INVITE USER
        // ============================================================
        [Fact]
        public async Task InviteUserAsync_Should_Send_Email_Invite()
        {
            var dto = new InviteUserDto
            {
                FullName = "John",
                Email = "invite@example.com",
                Role = "User"
            };

            _configMock.Setup(c => c["Client:Url"]).Returns("https://myapp.com");

            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), dto.Role))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("TOKEN123");

            Func<Task> act = async () => await _service.InviteUserAsync(dto);

            await act.Should().NotThrowAsync();
            _emailMock.Verify(e => e.SendAsync(dto.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // ============================================================
        // DEACTIVATE USER
        // ============================================================
        [Fact]
        public async Task DeactivateUserAsync_Should_Set_IsActive_False()
        {
            var user = new ApplicationUser { Id = "123", IsActive = true };

            _userManagerMock.Setup(m => m.FindByIdAsync("123")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            await _service.DeactivateUserAsync("123");

            user.IsActive.Should().BeFalse();
        }

        // ============================================================
        // GET USER BY ID
        // ============================================================
        [Fact]
        public async Task GetUserByIdAsync_Should_Return_ResponseDto()
        {
            var user = new ApplicationUser { Id = "10", Email = "x@y.com", FullName = "Test" };

            _repoMock.Setup(r => r.GetByIdAsync("10")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Admin" });

            var result = await _service.GetUserByIdAsync("10");

            result.Email.Should().Be("x@y.com");
            result.Role.Should().Be("Admin");
        }

        // ============================================================
        // MEANINGFUL FAILING TEST FOR PRESENTATION
        // ============================================================
        [Fact]
        public async Task CreateUserAsync_Should_Fail_When_Role_Assignment_Fails()
        {
            var dto = new CreateUserDto
            {
                FullName = "John",
                Email = "john@example.com",
                Role = "InvalidRole"
            };

            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), dto.Role))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role does not exist" }));

            // ❗ Expected fail (shows missing validation or role check)
            Func<Task> act = async () => await _service.CreateUserAsync(dto);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Role assignment failed");
        }
    }
}
