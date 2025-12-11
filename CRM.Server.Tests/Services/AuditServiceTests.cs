using System;
using System.Threading.Tasks;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CRM.Server.Tests.Services
{
    public class AuditLogServiceTests
    {
        private readonly Mock<IAuditRepository> _repoMock;
        private readonly AuditLogService _service;

        public AuditLogServiceTests()
        {
            _repoMock = new Mock<IAuditRepository>();
            _service = new AuditLogService(_repoMock.Object);
        }

        // --------------------------------------------------------
        // SUCCESS CASE: Should call AddAsync with correct AuditLog
        // --------------------------------------------------------
        [Fact]
        public async Task LogAsync_Should_Call_AddAsync_With_Correct_Data()
        {
            // Arrange
            AuditLog? capturedLog = null;

            _repoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                     .Callback<AuditLog>(log => capturedLog = log)
                     .Returns(Task.CompletedTask);

            // Act
            await _service.LogAsync(
                performedByUserId: "admin123",
                targetUserId: "user456",
                action: "Update",
                entityName: "User",
                isSuccess: true,
                ipAddress: "127.0.0.1",
                oldValue: "Old Name",
                newValue: "New Name"
            );

            // Assert
            _repoMock.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Once);
            capturedLog.Should().NotBeNull();

            capturedLog!.PerformedByUserId.Should().Be("admin123");
            capturedLog.TargetUserId.Should().Be("user456");
            capturedLog.Action.Should().Be("Update");
            capturedLog.EntityName.Should().Be("User");
            capturedLog.IsSuccess.Should().BeTrue();
            capturedLog.IpAddress.Should().Be("127.0.0.1");
            capturedLog.OldValue.Should().Be("Old Name");
            capturedLog.NewValue.Should().Be("New Name");
        }

        // --------------------------------------------------------
        // VALID CASE: Should allow null values
        // --------------------------------------------------------
        [Fact]
        public async Task LogAsync_Should_Work_With_Null_Optional_Values()
        {
            AuditLog? capturedLog = null;

            _repoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                     .Callback<AuditLog>(log => capturedLog = log)
                     .Returns(Task.CompletedTask);

            await _service.LogAsync(
                performedByUserId: null,
                targetUserId: null,
                action: "Login",
                entityName: "Auth",
                isSuccess: true
            );

            capturedLog.Should().NotBeNull();
            capturedLog!.PerformedByUserId.Should().BeNull();
            capturedLog.TargetUserId.Should().BeNull();
            capturedLog.Action.Should().Be("Login");
            capturedLog.EntityName.Should().Be("Auth");
            capturedLog.IsSuccess.Should().BeTrue();
            capturedLog.IpAddress.Should().BeNull();
            capturedLog.OldValue.Should().BeNull();
            capturedLog.NewValue.Should().BeNull();
        }

        // --------------------------------------------------------
        // FAILURE CASE: Repository throws exception
        // --------------------------------------------------------
        [Fact]
        public async Task LogAsync_Should_Throw_When_Repository_Throws()
        {
            _repoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                     .ThrowsAsync(new Exception("DB error"));

            Func<Task> act = async () => await _service.LogAsync(
                performedByUserId: "u1",
                targetUserId: "u2",
                action: "Delete",
                entityName: "User",
                isSuccess: false
            );

            await act.Should().ThrowAsync<Exception>()
                     .WithMessage("DB error");
        }

        // --------------------------------------------------------
        // MEANINGFUL FAILING TEST FOR PRESENTATION
        // --------------------------------------------------------
        [Fact]
        public async Task LogAsync_Should_Fail_When_Action_Is_Empty()
        {
            // Act
            Func<Task> act = async () => await _service.LogAsync(
                performedByUserId: "admin",
                targetUserId: "user1",
                action: "",       // ❌ INVALID ON PURPOSE
                entityName: "User",
                isSuccess: true
            );

            // EXPECT FAILING TEST → service does NOT validate action
            await act.Should().ThrowAsync<Exception>(
                "Action should not be empty — this is the intentional failing test"
            );
        }
    }
}
