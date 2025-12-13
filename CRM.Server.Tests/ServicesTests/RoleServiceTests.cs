using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace CRM.Tests.Services
{
    public class RoleServiceTests
    {
        private readonly Mock<IRoleRepository> _roleRepoMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IAuditLogService> _auditMock;
        private readonly RoleService _service;

        public RoleServiceTests()
        {
            _roleRepoMock = new Mock<IRoleRepository>();

            _userManagerMock = MockUserManager();

            _auditMock = new Mock<IAuditLogService>();

            _service = new RoleService(
                _roleRepoMock.Object,
                _userManagerMock.Object,
                _auditMock.Object
            );
        }

        // ----------------------------
        // Mock UserManager Helper
        // ----------------------------
        private Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

            return new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null, null, null, null, null, null, null, null
            );
        }

        // ----------------------------
        // CREATE ROLE
        // ----------------------------
        [Fact]
        public async Task CreateRoleAsync_ShouldCreateAndAudit()
        {
            // Arrange
            var performer = "admin";

            _roleRepoMock.Setup(r => r.CreateAsync("Manager"))
                .Returns(Task.CompletedTask);

            // Act
            await _service.CreateRoleAsync("Manager", performer);

            // Assert repo call
            _roleRepoMock.Verify(r => r.CreateAsync("Manager"), Times.Once);

            // Assert audit
            _auditMock.Verify(a => a.LogAsync(
                performer,
                null,
                "Role Created",
                "Role",
                true,
                null,
                null,
                It.Is<string>(s => s.Contains("Manager"))
            ), Times.Once);
        }

        [Fact]
        public async Task CreateRoleAsync_ShouldSwallowAuditFailures()
        {
            // Arrange
            _roleRepoMock.Setup(r => r.CreateAsync("Manager"))
                .Returns(Task.CompletedTask);

            _auditMock.Setup(a => a.LogAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()
            )).ThrowsAsync(new System.Exception("audit failed"));

            // Act
            var ex = await Record.ExceptionAsync(() =>
                _service.CreateRoleAsync("Manager", "admin")
            );

            // Assert
            Assert.Null(ex);
        }

        // ----------------------------
        // GET ALL ROLES
        // ----------------------------
        [Fact]
        public async Task GetAllRolesAsync_ReturnsRoles()
        {
            // Arrange
            var roles = new List<IdentityRole>
            {
                new IdentityRole("Admin"),
                new IdentityRole("Manager")
            };

            _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(roles);

            // Act
            var res = await _service.GetAllRolesAsync();

            // Assert
            Assert.Equal(2, res.Count);
            Assert.Contains(res, r => r.Name == "Admin");
        }

        // ----------------------------
        // GET ROLE BY NAME
        // ----------------------------
        [Fact]
        public async Task GetRoleAsync_WhenExists_ReturnsRole()
        {
            var role = new IdentityRole("Tester");

            _roleRepoMock.Setup(r => r.GetByNameAsync("Tester"))
                .ReturnsAsync(role);

            var result = await _service.GetRoleAsync("Tester");

            Assert.NotNull(result);
            Assert.Equal("Tester", result!.Name);
        }

        [Fact]
        public async Task GetRoleAsync_WhenMissing_ReturnsNull()
        {
            _roleRepoMock.Setup(r => r.GetByNameAsync("Tester"))
                .ReturnsAsync((IdentityRole?)null);

            var result = await _service.GetRoleAsync("Tester");

            Assert.Null(result);
        }

        // ----------------------------
        // UPDATE ROLE
        // ----------------------------
        [Fact]
        public async Task UpdateRoleAsync_WhenMissing_Throws()
        {
            _roleRepoMock.Setup(r => r.GetByNameAsync("Old"))
                .ReturnsAsync((IdentityRole?)null);

            await Assert.ThrowsAsync<System.Exception>(() =>
                _service.UpdateRoleAsync("Old", "New", "admin"));
        }

        [Fact]
        public async Task UpdateRoleAsync_UpdatesAndAudits()
        {
            // Arrange
            var role = new IdentityRole("Old");
            _roleRepoMock.Setup(r => r.GetByNameAsync("Old"))
                .ReturnsAsync(role);

            _roleRepoMock.Setup(r => r.UpdateAsync(role))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateRoleAsync("Old", "New", "admin");

            // Assert repo update
            _roleRepoMock.Verify(r => r.UpdateAsync(It.Is<IdentityRole>(x =>
                x.Name == "New" &&
                x.NormalizedName == "NEW"
            )), Times.Once);

            // Assert audit
            _auditMock.Verify(a => a.LogAsync(
                "admin",
                null,
                "Role Updated",
                "Role",
                true,
                null,
                It.Is<string>(s => s.Contains("Old")),
                It.Is<string>(s => s.Contains("New"))
            ), Times.Once);
        }

        // ----------------------------
        // DELETE ROLE
        // ----------------------------
        [Fact]
        public async Task DeleteRoleAsync_WhenMissing_Throws()
        {
            _roleRepoMock.Setup(r => r.GetByNameAsync("Manager"))
                .ReturnsAsync((IdentityRole?)null);

            await Assert.ThrowsAsync<System.Exception>(() =>
                _service.DeleteRoleAsync("Manager", "admin"));
        }

        [Fact]
        public async Task DeleteRoleAsync_DeletesAndAudits()
        {
            // Arrange
            var role = new IdentityRole("Manager");

            _roleRepoMock.Setup(r => r.GetByNameAsync("Manager"))
                .ReturnsAsync(role);

            _roleRepoMock.Setup(r => r.DeleteAsync(role))
                .Returns(Task.CompletedTask);

            // Act
            await _service.DeleteRoleAsync("Manager", "admin");

            // Assert repo call
            _roleRepoMock.Verify(r => r.DeleteAsync(role), Times.Once);

            // Assert audit
            _auditMock.Verify(a => a.LogAsync(
                "admin",
                null,
                "Role Deleted",
                "Role",
                true,
                null,
                It.Is<string>(s => s.Contains("Manager")),
                null
            ), Times.Once);
        }

        // ----------------------------
        // ASSIGN ROLE
        // ----------------------------
        [Fact]
        public async Task AssignRoleAsync_Throws_WhenRoleNameEmpty()
        {
            await Assert.ThrowsAsync<System.Exception>(() =>
                _service.AssignRoleAsync("user1", "", "admin"));
        }

        [Fact]
        public async Task AssignRoleAsync_Throws_WhenUserMissing()
        {
            _userManagerMock.Setup(u => u.FindByIdAsync("123"))
                .ReturnsAsync((ApplicationUser?)null);

            await Assert.ThrowsAsync<System.Exception>(() =>
                _service.AssignRoleAsync("123", "Admin", "admin"));
        }

        [Fact]
        public async Task AssignRoleAsync_Throws_WhenRoleMissing()
        {
            var user = new ApplicationUser();
            _userManagerMock.Setup(u => u.FindByIdAsync("123"))
                .ReturnsAsync(user);

            _roleRepoMock.Setup(r => r.GetByNameAsync("Admin"))
                .ReturnsAsync((IdentityRole?)null);

            await Assert.ThrowsAsync<System.Exception>(() =>
                _service.AssignRoleAsync("123", "Admin", "admin"));
        }

        [Fact]
        public async Task AssignRoleAsync_WhenAlreadyAssigned_DoesNothing()
        {
            var user = new ApplicationUser();
            var role = new IdentityRole("Admin");

            _userManagerMock.Setup(u => u.FindByIdAsync("123"))
                .ReturnsAsync(user);

            _roleRepoMock.Setup(r => r.GetByNameAsync("Admin"))
                .ReturnsAsync(role);

            _userManagerMock.Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Admin" });

            // Act
            await _service.AssignRoleAsync("123", "Admin", "admin");

            // No repo calls
            _auditMock.Verify(a => a.LogAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task AssignRoleAsync_AssignsAndAudits()
        {
            // Arrange
            var user = new ApplicationUser();
            var role = new IdentityRole("Admin");

            _userManagerMock.Setup(u => u.FindByIdAsync("123"))
                .ReturnsAsync(user);

            _roleRepoMock.Setup(r => r.GetByNameAsync("Admin"))
                .ReturnsAsync(role);

            _userManagerMock.Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "OldRole" });

            _userManagerMock.Setup(u => u.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(u => u.AddToRoleAsync(user, "Admin"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _service.AssignRoleAsync("123", "Admin", "admin");

            // Assert audit
            _auditMock.Verify(a => a.LogAsync(
                "admin",
                "123",
                "Role Assigned",
                "UserRole",
                true,
                null,
                It.Is<string>(s => s.Contains("OldRole")),
                It.Is<string>(s => s.Contains("Admin"))
            ), Times.Once);
        }
    }
}
