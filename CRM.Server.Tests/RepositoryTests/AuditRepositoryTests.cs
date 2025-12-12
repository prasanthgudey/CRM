using System;
using System.Linq;
using System.Threading.Tasks;
using CRM.Server.Data;
using CRM.Server.Models;
using CRM.Server.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRM.Server.Tests.Repositories
{
    public class AuditRepositoryTests
    {
        private ApplicationDbContext CreateDb()
        {
            return new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);
        }

        // -----------------------------
        // ADD LOG
        // -----------------------------
        [Fact]
        public async Task AddAsync_ShouldAddAuditLog()
        {
            var db = CreateDb();
            var repo = new AuditRepository(db);

            var log = new AuditLog
            {
                Action = "Login",
                EntityName = "Auth",
                IsSuccess = true,
                CreatedAt = DateTime.UtcNow
            };

            await repo.AddAsync(log);

            var saved = await db.AuditLogs.FirstOrDefaultAsync();

            Assert.NotNull(saved);
            Assert.Equal("Login", saved.Action);
            Assert.Equal("Auth", saved.EntityName);
            Assert.True(saved.IsSuccess);

            // EF Core auto-generates integer primary key
            Assert.True(saved.Id > 0);
        }

        // -----------------------------
        // GET ALL (ORDERED)
        // -----------------------------
        [Fact]
        public async Task GetAllAsync_ShouldReturnLogsOrderedByCreatedAt()
        {
            var db = CreateDb();
            var repo = new AuditRepository(db);

            var old = new AuditLog
            {
                Action = "Old",
                CreatedAt = DateTime.UtcNow.AddMinutes(-20)
            };

            var newer = new AuditLog
            {
                Action = "Newer",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            await db.AuditLogs.AddRangeAsync(old, newer);
            await db.SaveChangesAsync();

            var list = await repo.GetAllAsync();

            Assert.Equal(2, list.Count);
            Assert.Equal("Newer", list[0].Action); // most recent first
            Assert.Equal("Old", list[1].Action);
        }

        // -----------------------------
        // EMPTY LIST
        // -----------------------------
        [Fact]
        public async Task GetAllAsync_WhenNoLogs_ReturnsEmptyList()
        {
            var db = CreateDb();
            var repo = new AuditRepository(db);

            var list = await repo.GetAllAsync();

            Assert.Empty(list);
        }
    }
}
