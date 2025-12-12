using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Server.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CRM.Server.Tests.Repositories
{
    public class RoleRepositoryTests
    {
        private RoleManager<IdentityRole> CreateMockRoleManager(out Mock<IRoleStore<IdentityRole>> storeMock)
        {
            storeMock = new Mock<IRoleStore<IdentityRole>>();

            return new RoleManager<IdentityRole>(
                storeMock.Object,
                null,
                null,
                null,
                null);
        }

        // ------------------------------------------------------
        // GET ALL
        // ------------------------------------------------------
        [Fact]
        public async Task GetAllAsync_ShouldReturnAllRoles()
        {
            var storeMock = new Mock<IRoleStore<IdentityRole>>();
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                storeMock.Object, null, null, null, null);

            var roles = new List<IdentityRole>
            {
                new IdentityRole("Admin"),
                new IdentityRole("Manager")
            }.AsQueryable();

            roleManagerMock.Setup(r => r.Roles).Returns(roles);

            var repo = new RoleRepository(roleManagerMock.Object);

            var result = await repo.GetAllAsync();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Name == "Admin");
        }

        // ------------------------------------------------------
        // GET BY NAME
        // ------------------------------------------------------
        [Fact]
        public async Task GetByNameAsync_ShouldReturnCorrectRole()
        {
            var storeMock = new Mock<IRoleStore<IdentityRole>>();
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                storeMock.Object, null, null, null, null);

            var expected = new IdentityRole("Admin");

            roleManagerMock
                .Setup(r => r.FindByNameAsync("Admin"))
                .ReturnsAsync(expected);

            var repo = new RoleRepository(roleManagerMock.Object);

            var result = await repo.GetByNameAsync("Admin");

            Assert.NotNull(result);
            Assert.Equal("Admin", result!.Name);
        }

        // ------------------------------------------------------
        // CREATE ROLE
        // ------------------------------------------------------
        [Fact]
        public async Task CreateAsync_ShouldCreateRole()
        {
            var storeMock = new Mock<IRoleStore<IdentityRole>>();
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                storeMock.Object, null, null, null, null);

            roleManagerMock
                .Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
                .ReturnsAsync(IdentityResult.Success);

            var repo = new RoleRepository(roleManagerMock.Object);

            await repo.CreateAsync("NewRole");

            roleManagerMock.Verify(r =>
                r.CreateAsync(It.Is<IdentityRole>(x => x.Name == "NewRole")), Times.Once);
        }

        // ------------------------------------------------------
        // UPDATE ROLE
        // ------------------------------------------------------
        [Fact]
        public async Task UpdateAsync_ShouldUpdateRole()
        {
            var storeMock = new Mock<IRoleStore<IdentityRole>>();
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                storeMock.Object, null, null, null, null);

            var role = new IdentityRole("Old");

            roleManagerMock
                .Setup(r => r.UpdateAsync(role))
                .ReturnsAsync(IdentityResult.Success);

            var repo = new RoleRepository(roleManagerMock.Object);

            await repo.UpdateAsync(role);

            roleManagerMock.Verify(r => r.UpdateAsync(role), Times.Once);
        }

        // ------------------------------------------------------
        // DELETE ROLE
        // ------------------------------------------------------
        [Fact]
        public async Task DeleteAsync_ShouldDeleteRole()
        {
            var storeMock = new Mock<IRoleStore<IdentityRole>>();
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                storeMock.Object, null, null, null, null);

            var role = new IdentityRole("DeleteRole");

            roleManagerMock
                .Setup(r => r.DeleteAsync(role))
                .ReturnsAsync(IdentityResult.Success);

            var repo = new RoleRepository(roleManagerMock.Object);

            await repo.DeleteAsync(role);

            roleManagerMock.Verify(r => r.DeleteAsync(role), Times.Once);
        }
    }
}
