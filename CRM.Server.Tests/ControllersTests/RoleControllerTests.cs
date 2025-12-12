using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CRM.Server.Controllers;
using CRM.Server.DTOs.Roles;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CRM.Tests.Controllers
{
    public class RoleControllerTests
    {
        private readonly Mock<IRoleService> _serviceMock;
        private readonly RoleController _controller;

        public RoleControllerTests()
        {
            _serviceMock = new Mock<IRoleService>();

            _controller = new RoleController(_serviceMock.Object);

            // Fake user context for performedBy
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "admin-1")
                        })
                    )
                }
            };
        }

        // ---------------------------------------------------------
        // 1) Create Role
        // ---------------------------------------------------------
        [Fact]
        public async Task CreateRole_WhenValid_ReturnsOk()
        {
            var dto = new CreateRoleDto { RoleName = "Manager" };

            _serviceMock.Setup(s => s.GetRoleAsync("Manager"))
                .ReturnsAsync((Microsoft.AspNetCore.Identity.IdentityRole?)null);

            _serviceMock.Setup(s => s.CreateRoleAsync("Manager", "admin-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.CreateRole(dto) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal("Role created successfully", result.Value);
        }

        [Fact]
        public async Task CreateRole_WhenRoleExists_ReturnsConflict()
        {
            var dto = new CreateRoleDto { RoleName = "Admin" };

            _serviceMock.Setup(s => s.GetRoleAsync("Admin"))
                .ReturnsAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Admin"));

            var result = await _controller.CreateRole(dto);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task CreateRole_WhenEmptyName_ReturnsBadRequest()
        {
            var dto = new CreateRoleDto { RoleName = "" };

            var result = await _controller.CreateRole(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ---------------------------------------------------------
        // 2) Assign Role
        // ---------------------------------------------------------
        [Fact]
        public async Task AssignRole_WhenValid_ReturnsOk()
        {
            var dto = new AssignRoleDto { UserId = "u1", RoleName = "Admin" };

            _serviceMock.Setup(s => s.AssignRoleAsync("u1", "Admin", "admin-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.AssignRole(dto) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal("Role assigned successfully", result.Value);
        }

        [Fact]
        public async Task AssignRole_WhenServiceThrows_ReturnsBadRequest()
        {
            var dto = new AssignRoleDto { UserId = "u1", RoleName = "Admin" };

            _serviceMock.Setup(s => s.AssignRoleAsync("u1", "Admin", "admin-1"))
                .ThrowsAsync(new Exception("Failed"));

            var result = await _controller.AssignRole(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ---------------------------------------------------------
        // 3) GetAll
        // ---------------------------------------------------------
        [Fact]
        public async Task GetAll_ReturnsRoles()
        {
            var roles = new List<Microsoft.AspNetCore.Identity.IdentityRole>
            {
                new Microsoft.AspNetCore.Identity.IdentityRole("Admin"),
                new Microsoft.AspNetCore.Identity.IdentityRole("Manager")
            };

            _serviceMock.Setup(s => s.GetAllRolesAsync()).ReturnsAsync(roles);

            var result = await _controller.GetAll() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(roles, result.Value);
        }

        // ---------------------------------------------------------
        // 4) Get Role by Name
        // ---------------------------------------------------------
        [Fact]
        public async Task Get_WhenFound_ReturnsOk()
        {
            var role = new Microsoft.AspNetCore.Identity.IdentityRole("Admin");

            _serviceMock.Setup(s => s.GetRoleAsync("Admin"))
                .ReturnsAsync(role);

            var result = await _controller.Get("Admin") as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(role, result.Value);
        }

        [Fact]
        public async Task Get_WhenNotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetRoleAsync("X"))
                .ReturnsAsync((Microsoft.AspNetCore.Identity.IdentityRole?)null);

            var result = await _controller.Get("X");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ---------------------------------------------------------
        // 5) Update Role
        // ---------------------------------------------------------
        [Fact]
        public async Task Update_WhenValid_ReturnsOk()
        {
            var dto = new UpdateRoleDto { OldName = "User", NewName = "Member" };

            _serviceMock.Setup(s => s.GetRoleAsync("Member"))
                .ReturnsAsync((Microsoft.AspNetCore.Identity.IdentityRole?)null);

            _serviceMock.Setup(s => s.UpdateRoleAsync("User", "Member", "admin-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.Update(dto) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal("Role updated successfully", result.Value);
        }

        [Fact]
        public async Task Update_WhenNewNameExists_ReturnsConflict()
        {
            var dto = new UpdateRoleDto { OldName = "User", NewName = "Admin" };

            _serviceMock.Setup(s => s.GetRoleAsync("Admin"))
                .ReturnsAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Admin"));

            var result = await _controller.Update(dto);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Update_WhenOldAndNewSame_ReturnsNoContent()
        {
            var dto = new UpdateRoleDto { OldName = "User", NewName = "USER" };

            var result = await _controller.Update(dto);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Update_WhenMissingValues_ReturnsBadRequest()
        {
            var dto = new UpdateRoleDto { OldName = "", NewName = "X" };

            var result = await _controller.Update(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_WhenServiceThrows_ReturnsBadRequest()
        {
            var dto = new UpdateRoleDto { OldName = "User", NewName = "Member" };

            _serviceMock.Setup(s => s.GetRoleAsync("Member"))
                .ReturnsAsync((Microsoft.AspNetCore.Identity.IdentityRole?)null);

            _serviceMock.Setup(s => s.UpdateRoleAsync("User", "Member", "admin-1"))
                .ThrowsAsync(new Exception("Fail"));

            var result = await _controller.Update(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ---------------------------------------------------------
        // 6) DELETE ROLE
        // ---------------------------------------------------------
        [Fact]
        public async Task Delete_ReturnsOk()
        {
            _serviceMock.Setup(s => s.DeleteRoleAsync("User", "admin-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.Delete("User") as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal("Role deleted successfully", result.Value);
        }
    }
}
