using System;
using System.Threading.Tasks;
using CRM.Server.Data;
using CRM.Server.Models;
using CRM.Server.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRM.Server.Tests.Repositories
{
    public class UserRepositoryTests
    {
        private ApplicationDbContext CreateDb()
        {
            return new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);
        }

        // -------------------------------------------------------
        // ADD USER
        // -------------------------------------------------------
        [Fact]
        public async Task AddAsync_ShouldAddUser()
        {
            var db = CreateDb();
            var repo = new UserRepository(db);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "Test User",
                Email = "test@example.com",
                UserName = "test@example.com",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await repo.AddAsync(user);

            var saved = await db.Users.FirstOrDefaultAsync();

            Assert.NotNull(saved);
            Assert.Equal("Test User", saved.FullName);
            Assert.Equal("test@example.com", saved.Email);
        }

        // -------------------------------------------------------
        // GET BY ID
        // -------------------------------------------------------
        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectUser()
        {
            var db = CreateDb();
            var repo = new UserRepository(db);

            var id = Guid.NewGuid().ToString();

            var user = new ApplicationUser
            {
                Id = id,
                FullName = "Alice",
                Email = "alice@example.com",
                UserName = "alice@example.com",
                CreatedAt = DateTime.UtcNow
            };

            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();

            var result = await repo.GetByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal("Alice", result!.FullName);
        }

        // -------------------------------------------------------
        // GET BY EMAIL
        // -------------------------------------------------------
        [Fact]
        public async Task GetByEmailAsync_ShouldReturnUser()
        {
            var db = CreateDb();
            var repo = new UserRepository(db);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "Bob",
                Email = "bob@example.com",
                UserName = "bob@example.com",
                CreatedAt = DateTime.UtcNow
            };

            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();

            var result = await repo.GetByEmailAsync("bob@example.com");

            Assert.NotNull(result);
            Assert.Equal("Bob", result!.FullName);
        }

        // -------------------------------------------------------
        // GET ALL USERS
        // -------------------------------------------------------
        [Fact]
        public async Task GetAllAsync_ShouldReturnAllUsers()
        {
            var db = CreateDb();
            var repo = new UserRepository(db);

            await db.Users.AddAsync(new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "User A",
                Email = "a@example.com",
                UserName = "a@example.com",
                CreatedAt = DateTime.UtcNow
            });

            await db.Users.AddAsync(new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "User B",
                Email = "b@example.com",
                UserName = "b@example.com",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            var result = await repo.GetAllAsync();

            Assert.Equal(2, result.Count);
        }

        // -------------------------------------------------------
        // UPDATE USER
        // -------------------------------------------------------
        [Fact]
        public async Task UpdateAsync_ShouldModifyUser()
        {
            var db = CreateDb();
            var repo = new UserRepository(db);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "Old Name",
                Email = "old@example.com",
                UserName = "old@example.com",
                CreatedAt = DateTime.UtcNow
            };

            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();

            // Update
            user.FullName = "New Name";
            user.Email = "new@example.com";

            await repo.UpdateAsync(user);

            var updated = await db.Users.FirstAsync();

            Assert.Equal("New Name", updated.FullName);
            Assert.Equal("new@example.com", updated.Email);
        }

        // -------------------------------------------------------
        // DELETE USER
        // -------------------------------------------------------
        [Fact]
        public async Task DeleteAsync_ShouldRemoveUser()
        {
            var db = CreateDb();
            var repo = new UserRepository(db);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = "Delete Me",
                Email = "delete@example.com",
                UserName = "delete@example.com",
                CreatedAt = DateTime.UtcNow
            };

            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();

            await repo.DeleteAsync(user);

            Assert.Equal(0, await db.Users.CountAsync());
        }
    }
}
