using CRM.Server.Controllers;
using CRM.Server.DTOs.Roles;
using CRM.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CRM.Server.Tests.Controllers
{
    public class RoleControllerTests
    {
        private readonly Mock<IRoleService> _roleServiceMock;
        private readonly Mock<ILogger<RoleController>> _loggerMock;
        private readonly RoleController _controller;

        public RoleControllerTests()
        {
            _roleServiceMock = new Mock<IRoleService>();
            _loggerMock = new Mock<ILogger<RoleController>>();

            _controller = new RoleController(
                _roleServiceMock.Object,
                _loggerMock.Object
            );

            SetFakeUser();
        }

        // =============================================
        // CREATE ROLE
        // =============================================

        [Fact]
        public async Task CreateRole_WhenValid_ReturnsOk()
        {
            var dto = new CreateRoleDto
            {
                RoleName = "Admin"
            };

            _roleServiceMock
                .Setup(s => s.GetRoleAsync("ADMIN"))
                .Returns(Task.FromResult<IdentityRole?>(null));

            _roleServiceMock
                .Setup(s => s.CreateRoleAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.CreateRole(dto);

            result.Should().BeOfType<OkObjectResult>();
        }


        [Fact]
        public async Task CreateRole_WhenRoleNameMissing_ReturnsBadRequest()
        {
            var dto = new CreateRoleDto { RoleName = "" };

            var result = await _controller.CreateRole(dto);

            result.Should().BeOfType<BadRequestObjectResult>();
        }


        [Fact]
        public async Task CreateRole_WhenRoleAlreadyExists_ReturnsConflict()
        {
            var dto = new CreateRoleDto
            {
                RoleName = "Admin"
            };

            _roleServiceMock
                .Setup(s => s.GetRoleAsync("ADMIN"))
                .ReturnsAsync(new IdentityRole("ADMIN")); // ✅ correct type

            var result = await _controller.CreateRole(dto);

            result.Should().BeOfType<ConflictObjectResult>();
        }


        // =============================================
        // ASSIGN ROLE
        // =============================================

        [Fact]
        public async Task AssignRole_WhenValid_ReturnsOk()
        {
            var dto = new AssignRoleDto
            {
                UserId = "user-1",
                RoleName = "Admin"
            };

            _roleServiceMock
                .Setup(s => s.AssignRoleAsync(
                    dto.UserId,
                    dto.RoleName,
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.AssignRole(dto);

            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // GET ALL ROLES
        // =============================================

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            _roleServiceMock
                .Setup(s => s.GetRoleAsync("Admin"))
.ReturnsAsync(new IdentityRole("Admin"));


            var result = await _controller.GetAll();

            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // GET ROLE BY NAME
        // =============================================


        [Fact]
        public async Task Get_WhenRoleExists_ReturnsOk()
        {
            _roleServiceMock
                .Setup(s => s.GetRoleAsync("Admin"))
                .ReturnsAsync(new IdentityRole("Admin")); // ✅ correct type

            var result = await _controller.Get("Admin");

            result.Should().BeOfType<OkObjectResult>();
        }



        [Fact]
        public async Task Get_WhenRoleNotFound_ReturnsNotFound()
        {
            _roleServiceMock
                .Setup(s => s.GetRoleAsync("Admin"))
                .Returns(Task.FromResult<IdentityRole?>(null));

            var result = await _controller.Get("Admin");

            result.Should().BeOfType<NotFoundObjectResult>();
        }


        // =============================================
        // UPDATE ROLE
        // =============================================


        [Fact]
        public async Task Update_WhenValid_ReturnsOk()
        {
            var dto = new UpdateRoleDto
            {
                OldName = "Admin",
                NewName = "Manager"
            };

            _roleServiceMock
                .Setup(s => s.GetRoleAsync("MANAGER"))
                .Returns(Task.FromResult<IdentityRole?>(null)); // ✅ correct

            _roleServiceMock
                .Setup(s => s.UpdateRoleAsync(
                    "ADMIN",
                    "MANAGER",
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Update(dto);

            result.Should().BeOfType<OkObjectResult>();
        }


        [Fact]
        public async Task Update_WhenNewNameSameAsOld_ReturnsBadRequest()
        {
            var dto = new UpdateRoleDto
            {
                OldName = "Admin",
                NewName = "admin"
            };

            var result = await _controller.Update(dto);

            result.Should().BeOfType<BadRequestObjectResult>();
        }


        [Fact]
        public async Task Update_WhenNewRoleAlreadyExists_ReturnsConflict()
        {
            var dto = new UpdateRoleDto
            {
                OldName = "Admin",
                NewName = "Manager"
            };

            _roleServiceMock
                .Setup(s => s.GetRoleAsync("MANAGER"))
                .ReturnsAsync(new IdentityRole("MANAGER")); // ✅ correct

            var result = await _controller.Update(dto);

            result.Should().BeOfType<ConflictObjectResult>();
        }


        // =============================================
        // DELETE ROLE
        // =============================================

        [Fact]
        public async Task Delete_WhenValid_ReturnsOk()
        {
            _roleServiceMock
                .Setup(s => s.DeleteRoleAsync(
                    "Admin",
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Delete("Admin");

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
