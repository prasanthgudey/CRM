using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CRM.Server.Controllers;
using CRM.Server.DTOs.Roles;
using CRM.Server.DTOs.Users;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CRM.Server.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _service;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _service = new Mock<IUserService>();

            _controller = new UserController(_service.Object);

            // Fake logged-in Admin user
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(new Claim[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                            new Claim(ClaimTypes.Role, "Admin")
                        }, "TestAuth"))
                }
            };
        }

        // ------------------------------------------------------
        // CREATE USER
        // ------------------------------------------------------
        [Fact]
        public async Task CreateUser_WhenValid_ReturnsSuccessJson()
        {
            var dto = new CreateUserDto
            {
                FullName = "John",
                Email = "j@crm.com",
                Role = "User"
            };

            _service.Setup(s => s.CreateUserAsync(dto, "admin-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.CreateUser(dto) as OkObjectResult;

            Assert.NotNull(result);
            dynamic json = result.Value;
            Assert.True(json.success);
            Assert.Equal("User created successfully", json.message);
        }

        [Fact]
        public async Task CreateUser_WhenServiceThrows_ReturnsSuccessFalseJson()
        {
            var dto = new CreateUserDto { Email = "exists@crm.com" };

            _service.Setup(s => s.CreateUserAsync(dto, "admin-1"))
                .ThrowsAsync(new System.Exception("Email already exists"));

            var result = await _controller.CreateUser(dto) as OkObjectResult;

            Assert.NotNull(result);
            dynamic json = result.Value;
            Assert.False(json.success);
            Assert.Equal("Email already exists", json.message);
        }

        // ------------------------------------------------------
        // GET ALL USERS
        // ------------------------------------------------------
        [Fact]
        public async Task GetAllUsers_ReturnsList()
        {
            var users = new List<UserResponseDto>
            {
                new UserResponseDto{ Id="u1", FullName="A", Email="a@crm.com"},
                new UserResponseDto{ Id="u2", FullName="B", Email="b@crm.com"}
            };

            _service.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(users);

            var result = await _controller.GetAllUsers() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(users, result.Value);
        }

        // ------------------------------------------------------
        // INVITE USER
        // ------------------------------------------------------
        [Fact]
        public async Task InviteUser_WhenValid_ReturnsSuccessJson()
        {
            var dto = new InviteUserDto
            {
                FullName = "New",
                Email = "new@crm.com",
                Role = "User"
            };

            _service.Setup(s => s.InviteUserAsync(dto, "admin-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.InviteUser(dto) as OkObjectResult;

            Assert.NotNull(result);
            dynamic json = result.Value;
            Assert.True(json.success);
            Assert.Equal("Invitation sent successfully", json.message);
        }

        [Fact]
        public async Task InviteUser_WhenServiceThrows_ReturnsSuccessFalseJson()
        {
            var dto = new InviteUserDto { Email = "exists@crm.com" };

            _service.Setup(s => s.InviteUserAsync(dto, "admin-1"))
                .ThrowsAsync(new System.Exception("Email already exists"));

            var result = await _controller.InviteUser(dto) as OkObjectResult;

            Assert.NotNull(result);
            dynamic json = result.Value;
            Assert.False(json.success);
            Assert.Equal("Email already exists", json.message);
        }

        // ------------------------------------------------------
        // ACTIVATE & DEACTIVATE USER
        // ------------------------------------------------------
        [Fact]
        public async Task Deactivate_ReturnsSuccessMessage()
        {
            _service.Setup(s => s.DeactivateUserAsync("u1", "admin-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.Deactivate("u1") as OkObjectResult;

            Assert.NotNull(result);
            dynamic json = result.Value;
            Assert.Equal("User deactivated successfully", json.message);
        }

        [Fact]
        public async Task Activate_ReturnsSuccessMessage()
        {
            _service.Setup(s => s.ActivateUserAsync("u1", "admin-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.Activate("u1") as OkObjectResult;

            Assert.NotNull(result);
            dynamic json = result.Value;
            Assert.Equal("User activated successfully", json.message);
        }

        // ------------------------------------------------------
        // UPDATE USER
        // ------------------------------------------------------
        [Fact]
        public async Task UpdateUser_ReturnsSuccessMessage()
        {
            var dto = new UpdateUserDto { FullName = "Updated" };

            _service.Setup(s => s.UpdateUserAsync("u1", dto, "admin-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.UpdateUser("u1", dto) as OkObjectResult;

            Assert.NotNull(result);
            dynamic json = result.Value;
            Assert.Equal("User updated successfully", json.message);
        }

        // ------------------------------------------------------
        // DELETE USER
        // ------------------------------------------------------
        [Fact]
        public async Task DeleteUser_ReturnsSuccessMessage()
        {
            _service.Setup(s => s.DeleteUserAsync("u1", "admin-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteUser("u1") as OkObjectResult;

            Assert.NotNull(result);
            dynamic json = result.Value;
            Assert.Equal("User deleted successfully", json.message);
        }

        // ------------------------------------------------------
        // GET USER BY ID
        // ------------------------------------------------------
        [Fact]
        public async Task GetUserById_WhenFound_ReturnsUser()
        {
            var user = new UserResponseDto { Id = "u1", FullName = "Test" };

            _service.Setup(s => s.GetUserByIdAsync("u1")).ReturnsAsync(user);

            var result = await _controller.GetUserById("u1") as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(user, result.Value);
        }

        [Fact]
        public async Task GetUserById_WhenNotFound_ReturnsBadRequest()
        {
            _service.Setup(s => s.GetUserByIdAsync("u1"))
                .ThrowsAsync(new System.Exception("User not found"));

            var result = await _controller.GetUserById("u1");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ------------------------------------------------------
        // GET MY PROFILE
        // ------------------------------------------------------
        [Fact]
        public async Task GetMyProfile_ReturnsLoggedInUser()
        {
            var user = new UserResponseDto { Id = "admin-1", FullName = "Admin User" };

            _service.Setup(s => s.GetUserByIdAsync("admin-1"))
                .ReturnsAsync(user);

            var result = await _controller.GetMyProfile() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(user, result.Value);
        }

        // ------------------------------------------------------
        // ASSIGN ROLE
        // ------------------------------------------------------
        [Fact]
        public async Task AssignRole_ReturnsSuccessMessage()
        {
            var dto = new AssignRoleDto { UserId = "u1", RoleName = "Admin" };

            _service.Setup(s => s.AssignRoleAsync("u1", "Admin", "admin-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.AssignRole(dto) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal("Role assigned successfully", result.Value);
        }

        [Fact]
        public async Task AssignRole_WhenServiceThrows_ReturnsBadRequest()
        {
            var dto = new AssignRoleDto { UserId = "u1", RoleName = "Admin" };

            _service.Setup(s => s.AssignRoleAsync("u1", "Admin", "admin-1"))
                .ThrowsAsync(new System.Exception("Role does not exist"));

            var result = await _controller.AssignRole(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ------------------------------------------------------
        // FILTER
        // ------------------------------------------------------
        [Fact]
        public async Task Filter_ReturnsFilteredUsers()
        {
            var filtered = new List<UserResponseDto>
            {
                new UserResponseDto { Id="u1", Role="Admin", IsActive=true }
            };

            _service.Setup(s => s.FilterUsersAsync("Admin", true))
                .ReturnsAsync(filtered);

            var result = await _controller.Filter("Admin", true) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(filtered, result.Value);
        }
    }
}
