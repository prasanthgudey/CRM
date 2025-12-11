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
        // SUCCESS CASE: Should create an AuditLog and call AddAsync
        // --------------------------------------------------------
        [Fact]
        public async Task LogAsync_Should_Call_AddAsync_With_Correct_Data()
        {
            // Arrange
            string userId = "user123";
            string action = "Updated";
            string oldValue = "Old Data";
            string newValue = "New Data";

            AuditLog? capturedLog = null;

            _repoMock
                .Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => capturedLog = log)
                .Returns(Task.CompletedTask);

            // Act
            await _service.LogAsync(userId, action, oldValue, newValue);

            // Assert
            _repoMock.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Once);

            capturedLog.Should().NotBeNull();
            capturedLog!.UserId.Should().Be(userId);
            capturedLog!.Action.Should().Be(action);
            capturedLog!.OldValue.Should().Be(oldValue);
            capturedLog!.NewValue.Should().Be(newValue);
        }

        // --------------------------------------------------------
        // NULL OLD/NEW VALUES: Should still log properly
        // --------------------------------------------------------
        [Fact]
        public async Task LogAsync_Should_Allow_Null_Values()
        {
            // Arrange
            string userId = "user123";
            string action = "Created";

            AuditLog? capturedLog = null;

            _repoMock
                .Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => capturedLog = log)
                .Returns(Task.CompletedTask);

            // Act
            await _service.LogAsync(userId, action);

            // Assert
            capturedLog.Should().NotBeNull();
            capturedLog!.UserId.Should().Be(userId);
            capturedLog!.Action.Should().Be(action);
            capturedLog!.OldValue.Should().BeNull();
            capturedLog!.NewValue.Should().BeNull();
        }

        // --------------------------------------------------------
        // FAILURE CASE: Simulate repository failure
        // --------------------------------------------------------
        [Fact]
        public async Task LogAsync_Should_Throw_When_Repository_Throws()
        {
            // Arrange
            _repoMock
                .Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            Func<Task> act = async () => await _service.LogAsync("u1", "Delete");

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database error");
        }

        // --------------------------------------------------------
        // MEANINGFUL FAILING TEST FOR PRESENTATION
        // --------------------------------------------------------
        [Fact]
        public async Task LogAsync_Should_Fail_When_UserId_Is_Empty()
        {
            // Arrange
            string userId = ""; // INVALID on purpose
            string action = "Update";

            // Act
            Func<Task> act = async () => await _service.LogAsync(userId, action);

            // ❗ EXPECTED TO FAIL ON PURPOSE
            // Because your service does NOT validate empty userId.
            // This test demonstrates missing validation.
            await act.Should().ThrowAsync<Exception>(
                "UserId should not be empty (EXPECTED FAILING TEST)"
            );
        }
    }
}
