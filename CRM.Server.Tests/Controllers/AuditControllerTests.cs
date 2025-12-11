using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRM.Server.Controllers;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FluentAssertions;

namespace CRM.Server.Tests.Controllers
{
    public class AuditControllerTests
    {
        private readonly Mock<IAuditRepository> _auditRepoMock;
        private readonly AuditController _controller;

        public AuditControllerTests()
        {
            _auditRepoMock = new Mock<IAuditRepository>();
            _controller = new AuditController(_auditRepoMock.Object);
        }

        // ---------------------------------------------------------
        // SUCCESS: Should return list of audit logs
        // ---------------------------------------------------------
        [Fact]
        public async Task GetAllLogs_Should_Return_Ok_With_AuditLogs()
        {
            // Arrange
            var logs = new List<AuditLog>
            {
                new AuditLog
                {
                    Id = 1,
                    UserId = "123",
                    Action = "UserCreated",
                    EntityName = "User",
                    OldValue = null,
                    NewValue = "{Name: 'John'}",
                    IpAddress = "127.0.0.1",
                    CreatedAt = DateTime.UtcNow
                },
                new AuditLog
                {
                    Id = 2,
                    UserId = "456",
                    Action = "RoleUpdated",
                    EntityName = "Role",
                    OldValue = "{Name:'User'}",
                    NewValue = "{Name:'Admin'}",
                    IpAddress = "127.0.0.1",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10)
                }
            };

            _auditRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(logs);

            // Act
            var result = await _controller.GetAllLogs();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            ok.Value.Should().BeEquivalentTo(logs);
        }

        // ---------------------------------------------------------
        // SUCCESS: Should return empty list
        // ---------------------------------------------------------
        [Fact]
        public async Task GetAllLogs_Should_Return_EmptyList_When_No_Logs()
        {
            // Arrange
            var emptyLogs = new List<AuditLog>();

            _auditRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(emptyLogs);

            // Act
            var result = await _controller.GetAllLogs();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            ok.Value.Should().BeEquivalentTo(emptyLogs);
        }

        // ---------------------------------------------------------
        // MEANINGFUL FAILING TEST (for presentation)
        // ---------------------------------------------------------
        [Fact]
        public async Task GetAllLogs_Should_Fail_When_Expecting_AtLeast_One_Log()
        {
            // Arrange: Case where no logs exist
            var emptyLogs = new List<AuditLog>();

            _auditRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(emptyLogs);

            // Act
            var result = await _controller.GetAllLogs();
            var ok = result as OkObjectResult;
            var returnedLogs = ok!.Value as List<AuditLog>;

            // ❌ Intentionally false expectation for presentation
            returnedLogs!.Count.Should().BeGreaterThan(0,
                "we expected at least one audit log to always exist for system tracking");
        }
    }
}
