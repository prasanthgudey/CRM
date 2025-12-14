using CRM.Server.Controllers;
using CRM.Server.DTOs.Auth;
using CRM.Server.DTOs.Roles;
using CRM.Server.DTOs.Users;
using CRM.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CRM.Server.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ILogger<UserController>> _loggerMock;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _loggerMock = new Mock<ILogger<UserController>>();

            _controller = new UserController(
                _userServiceMock.Object,
                _loggerMock.Object);

            SetFakeUser();
        }

        // =============================================
        // CREATE USER
        // =============================================

        [Fact]
        public async Task CreateUser_WhenValid_ReturnsOk()
        {
            var dto = new CreateUserDto
            {
                Email = "test@test.com"
            };

            _userServiceMock
                .Setup(s => s.GetAllUsersAsync())
                .ReturnsAsync(new List<UserResponseDto>());

            _userServiceMock
                .Setup(s => s.CreateUserAsync(
                    It.IsAny<CreateUserDto>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.CreateUser(dto);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CreateUser_WhenDuplicateEmail_ReturnsOkWithFailure()
        {
            var dto = new CreateUserDto
            {
                Email = "test@test.com"
            };

            _userServiceMock
                .Setup(s => s.GetAllUsersAsync())
                .ReturnsAsync(new List<UserResponseDto>
                {
                    new UserResponseDto { Email = "test@test.com" }
                });

            var result = await _controller.CreateUser(dto);

            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // GET ALL USERS
        // =============================================

        [Fact]
        public async Task GetAllUsers_ReturnsOk()
        {
            _userServiceMock
                .Setup(s => s.GetAllUsersAsync())
                .ReturnsAsync(new List<UserResponseDto>());

            var result = await _controller.GetAllUsers();

            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // INVITE USER
        // =============================================

        [Fact]
        public async Task InviteUser_WhenValid_ReturnsOk()
        {
            var dto = new InviteUserDto
            {
                Email = "invite@test.com"
            };

            _userServiceMock
                .Setup(s => s.GetAllUsersAsync())
                .ReturnsAsync(new List<UserResponseDto>());

            _userServiceMock
                .Setup(s => s.InviteUserAsync(
                    It.IsAny<InviteUserDto>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.InviteUser(dto);

            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // ACTIVATE / DEACTIVATE
        // =============================================

        [Fact]
        public async Task Deactivate_ReturnsOk()
        {
            _userServiceMock
                .Setup(s => s.DeactivateUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Deactivate("user1");

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Activate_ReturnsOk()
        {
            _userServiceMock
                .Setup(s => s.ActivateUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Activate("user1");

            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // UPDATE USER
        // =============================================

        [Fact]
        public async Task UpdateUser_WhenValid_ReturnsOk()
        {
            var dto = new UpdateUserDto
            {
                Email = "new@test.com"
            };

            _userServiceMock
                .Setup(s => s.GetAllUsersAsync())
                .ReturnsAsync(new List<UserResponseDto>());

            _userServiceMock
                .Setup(s => s.UpdateUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<UpdateUserDto>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.UpdateUser("user1", dto);

            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // DELETE USER
        // =============================================

        [Fact]
        public async Task DeleteUser_ReturnsOk()
        {
            _userServiceMock
                .Setup(s => s.DeleteUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteUser("user1");

            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // GET USER BY ID / ME
        // =============================================

        [Fact]
        public async Task GetUserById_ReturnsOk()
        {
            _userServiceMock
                .Setup(s => s.GetUserByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new UserResponseDto());

            var result = await _controller.GetUserById("user1");

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetMyProfile_ReturnsOk()
        {
            _userServiceMock
                .Setup(s => s.GetUserByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new UserResponseDto());

            var result = await _controller.GetMyProfile();

            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // ASSIGN ROLE
        // =============================================

        [Fact]
        public async Task AssignRole_ReturnsOk()
        {
            var dto = new AssignRoleDto
            {
                UserId = "user1",
                RoleName = "Admin"
            };

            _userServiceMock
                .Setup(s => s.AssignRoleAsync(
                    dto.UserId,
                    dto.RoleName,
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.AssignRole(dto);

            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // FILTER USERS
        // =============================================

        [Fact]
        public async Task Filter_ReturnsOk()
        {
            _userServiceMock
                .Setup(s => s.FilterUsersAsync(
                    It.IsAny<string?>(),
                    It.IsAny<bool?>()))
                .ReturnsAsync(new List<UserResponseDto>());

            var result = await _controller.Filter("Admin", true);

            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // HELPER
        // =============================================

        private void SetFakeUser()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };
        }
    }
}
