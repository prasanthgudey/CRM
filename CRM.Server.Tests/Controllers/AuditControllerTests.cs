using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Server.Controllers;
using CRM.Server.DTOs.Audit;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CRM.Server.Tests.Controllers
{
    public class AuditControllerTests
    {
        private readonly Mock<IAuditRepository> _auditRepoMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly AuditController _controller;

        public AuditControllerTests()
        {
            _auditRepoMock = new Mock<IAuditRepository>();

            // -------------------------------
            // Mock UserManager<ApplicationUser>
            // -------------------------------
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );

            _controller = new AuditController(_auditRepoMock.Object, _userManagerMock.Object);
        }

        // ---------------------------------------------------------
        // SUCCESS: Should return audit logs mapped to DTO
        // ---------------------------------------------------------
        [Fact]
        public async Task GetAllLogs_Should_Return_Ok_With_Mapped_AuditLogs()
        {
            // Test logs
            var logs = new List<AuditLog>
            {
                new AuditLog
                {
                    Id = 1,
                    PerformedByUserId = "123",
                    TargetUserId = "456",
                    Action = "UserCreated",
                    EntityName = "User",
                    NewValue = "{Name:'John'}",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _auditRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(logs);

            // Fake users for mapping
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "123", FullName = "Admin User" },
                new ApplicationUser { Id = "456", FullName = "Target User" }
            }.AsQueryable();

            _userManagerMock.Setup(u => u.Users).Returns(users);

            // Act
            var result = await _controller.GetAllLogs();

            // Assert
            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();

            var dtoList = ok!.Value as List<AuditLogResponseDto>;
            dtoList.Should().NotBeNull();
            dtoList.Should().HaveCount(1);

            var dto = dtoList![0];
            dto.PerformedByUserName.Should().Be("Admin User");
            dto.TargetUserName.Should().Be("Target User");
            dto.Action.Should().Be("UserCreated");
        }

        // ---------------------------------------------------------
        // SUCCESS: Should return empty DTO list
        // ---------------------------------------------------------
        [Fact]
        public async Task GetAllLogs_Should_Return_EmptyList_When_No_Logs()
        {
            _auditRepoMock.Setup(r => r.GetAllAsync())
                          .ReturnsAsync(new List<AuditLog>());

            _userManagerMock.Setup(u => u.Users)
                            .Returns(new List<ApplicationUser>().AsQueryable());

            var result = await _controller.GetAllLogs();
            var ok = result as OkObjectResult;

            var dtoList = ok!.Value as List<AuditLogResponseDto>;
            dtoList.Should().NotBeNull();
            dtoList.Should().BeEmpty();
        }

        // ---------------------------------------------------------
        // MEANINGFUL FAILING TEST
        // ---------------------------------------------------------
        [Fact]
        public async Task GetAllLogs_Should_Fail_When_Expecting_AtLeast_One_Log()
        {
            // Arrange
            _auditRepoMock.Setup(r => r.GetAllAsync())
                          .ReturnsAsync(new List<AuditLog>());

            _userManagerMock.Setup(u => u.Users)
                            .Returns(new List<ApplicationUser>().AsQueryable());

            // Act
            var result = await _controller.GetAllLogs();
            var ok = result as OkObjectResult;

            var dtoList = ok!.Value as List<AuditLogResponseDto>;

            // ❌ Intentional failing test for presentation
            dtoList!.Count.Should().BeGreaterThan(0,
                "System should always have at least 1 audit log (intentional failing test)"
            );
        }
    }
}
