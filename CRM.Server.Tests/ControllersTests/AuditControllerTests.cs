using CRM.Server.Controllers;
using CRM.Server.DTOs.Audit;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections;
using System.Linq.Expressions;
using Xunit;

namespace CRM.Tests.Controllers
{
    public class AuditControllerTests
    {
        private readonly Mock<IAuditRepository> _repoMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly AuditController _controller;

        public AuditControllerTests()
        {
            _repoMock = new Mock<IAuditRepository>();
            _userManagerMock = MockUserManager();

            _controller = new AuditController(_repoMock.Object, _userManagerMock.Object);
        }

        // ---------------------------
        // Async IQueryable Mock Setup
        // ---------------------------
        public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
        {
            public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
            public TestAsyncEnumerable(Expression expression) : base(expression) { }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;
            public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

            public T Current => _inner.Current;

            public ValueTask DisposeAsync()
            {
                _inner.Dispose();
                return ValueTask.CompletedTask;
            }

            public ValueTask<bool> MoveNextAsync()
                => new ValueTask<bool>(_inner.MoveNext());
        }

        // Mock user manager with async IQueryable support
        private Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );
        }

        private AuditLog Log(int id, string performedBy = "u1", string target = "u2")
        {
            return new AuditLog
            {
                Id = id,
                Action = "TEST",
                EntityName = "User",
                CreatedAt = DateTime.UtcNow,
                PerformedByUserId = performedBy,
                TargetUserId = target,
                IsSuccess = true,
                OldValue = "old",
                NewValue = "new"
            };
        }

        // ---------------------------------------------------------
        // 1) GetAllLogs()
        // ---------------------------------------------------------
        [Fact]
        public async Task GetAllLogs_ReturnsMappedData()
        {
            // Arrange logs
            var logs = new List<AuditLog>
            {
                Log(1, "u1", "u2"),
                Log(2, "u2", "u1")
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(logs);

            // Arrange users
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "u1", FullName = "User One" },
                new ApplicationUser { Id = "u2", FullName = "User Two" }
            };

            var asyncUsers = new TestAsyncEnumerable<ApplicationUser>(users);
            _userManagerMock.Setup(u => u.Users).Returns(asyncUsers);

            // Act
            var result = await _controller.GetAllLogs() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var dtoList = result.Value as List<AuditLogResponseDto>;

            Assert.Equal(2, dtoList!.Count);
            Assert.Equal("User One", dtoList[0].PerformedByUserName);
            Assert.Equal("User Two", dtoList[0].TargetUserName);
        }

        // ---------------------------------------------------------
        // 2) GetTotalCount()
        // ---------------------------------------------------------
        [Fact]
        public async Task GetTotalCount_ReturnsCorrectValue()
        {
            _repoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<AuditLog> { Log(1), Log(2), Log(3) });

            var res = await _controller.GetTotalCount() as OkObjectResult;

            Assert.NotNull(res);
            Assert.Equal(3, res.Value);
        }

        // ---------------------------------------------------------
        // 3) GetRecent()
        // ---------------------------------------------------------
        [Fact]
        public async Task GetRecent_ReturnsDescendingOrderedResults()
        {
            var logs = new List<AuditLog>
            {
                new AuditLog { Id = 1, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new AuditLog { Id = 2, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new AuditLog { Id = 3, CreatedAt = DateTime.UtcNow.AddMinutes(-1) }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(logs);

            var asyncUsers = new TestAsyncEnumerable<ApplicationUser>(new List<ApplicationUser>());
            _userManagerMock.Setup(u => u.Users).Returns(asyncUsers);

            var result = await _controller.GetRecent(2) as OkObjectResult;
            var list = result!.Value as List<AuditLogResponseDto>;

            Assert.Equal(2, list!.Count);
            Assert.Equal(3, list[0].Id); // newest
            Assert.Equal(2, list[1].Id);
        }

        // ---------------------------------------------------------
        // 4) GetErrorCount()
        // ---------------------------------------------------------
        [Fact]
        public async Task GetErrorCount_ReturnsOnlyFailedInRange()
        {
            var logs = new List<AuditLog>
            {
                new AuditLog { Id = 1, IsSuccess = false, CreatedAt = DateTime.UtcNow },
                new AuditLog { Id = 2, IsSuccess = false, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new AuditLog { Id = 3, IsSuccess = true }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(logs);

            var result = await _controller.GetErrorCount(7) as OkObjectResult;

            Assert.Equal(2, result!.Value); // 2 failed in last 7 days
        }
    }
}
