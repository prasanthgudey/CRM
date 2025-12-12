using Xunit;
using Moq;
using CRM.Server.Services;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CRM.Tests.Services
{
    public class AuditLogServiceTests
    {
        private readonly Mock<IAuditRepository> _auditRepoMock;
        private readonly AuditLogService _service;

        public AuditLogServiceTests()
        {
            _auditRepoMock = new Mock<IAuditRepository>();
            _service = new AuditLogService(_auditRepoMock.Object);
        }

        // -------------------------------------------------------
        // TEST 1: LogAsync should call AddAsync() once
        // -------------------------------------------------------
        [Fact]
        public async Task LogAsync_Should_Call_AddAsync_With_Correct_AuditLog()
        {
            // Arrange
            string performedBy = "user123";
            string targetUser = "target456";
            string action = "CREATE";
            string entity = "Customer";
            bool isSuccess = true;
            string ip = "127.0.0.1";
            string oldValue = "Old";
            string newValue = "New";

            AuditLog capturedLog = null!;

            _auditRepoMock
                .Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => capturedLog = log)
                .Returns(Task.CompletedTask);

            // Act
            await _service.LogAsync(performedBy, targetUser, action, entity, isSuccess, ip, oldValue, newValue);

            // Assert
            _auditRepoMock.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Once);

            Assert.Equal(performedBy, capturedLog.PerformedByUserId);
            Assert.Equal(targetUser, capturedLog.TargetUserId);
            Assert.Equal(action, capturedLog.Action);
            Assert.Equal(entity, capturedLog.EntityName);
            Assert.Equal(isSuccess, capturedLog.IsSuccess);
            Assert.Equal(ip, capturedLog.IpAddress);
            Assert.Equal(oldValue, capturedLog.OldValue);
            Assert.Equal(newValue, capturedLog.NewValue);
            Assert.NotEqual(default, capturedLog.CreatedAt); // Should have timestamp
        }

        // -------------------------------------------------------
        // TEST 2: GetTotalCount should return correct count
        // -------------------------------------------------------
        [Fact]
        public async Task GetTotalCountAsync_Should_Return_Correct_Count()
        {
            // Arrange
            var logs = new List<AuditLog>
            {
                new AuditLog(), new AuditLog(), new AuditLog()
            };

            _auditRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(logs);

            // Act
            var result = await _service.GetTotalCountAsync();

            // Assert
            Assert.Equal(3, result);
        }

        // -------------------------------------------------------
        // TEST 3: GetTotalCount should return 0 if repository returns null
        // -------------------------------------------------------
        [Fact]
        public async Task GetTotalCountAsync_Should_Return_Zero_When_List_Is_Null()
        {
            // Arrange
            _auditRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync((List<AuditLog>?)null);

            // Act
            var result = await _service.GetTotalCountAsync();

            // Assert
            Assert.Equal(0, result);
        }

        // -------------------------------------------------------
        // TEST 4: GetTotalCount should return 0 if list is empty
        // -------------------------------------------------------
        [Fact]
        public async Task GetTotalCountAsync_Should_Return_Zero_When_List_Is_Empty()
        {
            // Arrange
            _auditRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<AuditLog>());

            // Act
            var result = await _service.GetTotalCountAsync();

            // Assert
            Assert.Equal(0, result);
        }
    }
}
