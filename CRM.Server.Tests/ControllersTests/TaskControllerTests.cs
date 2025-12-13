using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using CRM.Server.Controllers;
using CRM.Server.Common.Paging;
using CRM.Server.Dtos;
using CRM.Server.Services;

namespace CRM.Server.Tests.Controllers
{
    public class TasksControllerTests
    {
        private readonly Mock<ITaskService> _serviceMock = new();
        private readonly Mock<ILogger<TasksController>> _loggerMock = new();

        private TasksController CreateController(ClaimsPrincipal? user = null)
        {
            var controller = new TasksController(_serviceMock.Object, _loggerMock.Object);

            var ctx = new DefaultHttpContext();
            if (user != null) ctx.User = user;
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            return controller;
        }

        private static ClaimsPrincipal MakeUser(string id = "user-1")
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, id) };
            var identity = new ClaimsIdentity(claims, "Test");
            return new ClaimsPrincipal(identity);
        }

        private static TaskResponseDto MakeTask(Guid? id = null, Guid? customerId = null, string userId = "user-1", string title = "T")
            => new TaskResponseDto
            {
                TaskId = id ?? Guid.NewGuid(),
                CustomerId = customerId ?? Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Description = "desc",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = Models.Tasks.TaskPriority.Medium,
                State = Models.Tasks.TaskState.Pending,
                CreatedAt = DateTime.UtcNow,
                IsRecurring = false,
                RecurrenceType = Models.Tasks.RecurrenceType.None
            };

        [Fact]
        public async Task GetAll_Paged_ReturnsOkWithPagedResult()
        {
            // Arrange
            var controller = CreateController();
            var parms = new PageParams { Page = 1, PageSize = 10, Search = "x" };

            var paged = new PagedResult<TaskResponseDto>
            {
                Items = new List<TaskResponseDto> { MakeTask(), MakeTask() },
                Page = 1,
                PageSize = 10,
                TotalCount = 2
            };

            _serviceMock.Setup(s => s.GetPagedAsync(It.Is<PageParams>(p => p.Page == parms.Page && p.PageSize == parms.PageSize && p.Search == parms.Search)))
                        .ReturnsAsync(paged);

            // Act
            var result = await controller.GetAll(parms);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ok.Value.Should().BeAssignableTo<PagedResult<TaskResponseDto>>();
            var returned = (PagedResult<TaskResponseDto>)ok.Value;
            returned.TotalCount.Should().Be(2);
            _serviceMock.Verify(s => s.GetPagedAsync(It.IsAny<PageParams>()), Times.Once);
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithList()
        {
            // Arrange
            var list = new List<TaskResponseDto> { MakeTask(), MakeTask() };
            _serviceMock.Setup(s => s.GetAllAsync(null)).ReturnsAsync(list);

            var controller = CreateController();

            // Act
            var result = await controller.GetAll();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ((IEnumerable<TaskResponseDto>)ok.Value!).Should().HaveCount(2);
            _serviceMock.Verify(s => s.GetAllAsync(null), Times.Once);
        }

        [Fact]
        public async Task GetAll_WhenServiceThrows_Returns500()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetAllAsync(null)).ThrowsAsync(new InvalidOperationException("fail"));
            var controller = CreateController();

            // Act
            var result = await controller.GetAll();

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var obj = (ObjectResult)result;
            obj.StatusCode.Should().Be(500);
            obj.Value.Should().Be("Failed to get tasks");
        }

        [Fact]
        public async Task GetById_WhenFound_ReturnsOk()
        {
            // Arrange
            var t = MakeTask();
            _serviceMock.Setup(s => s.GetByIdAsync(t.TaskId)).ReturnsAsync(t);

            var controller = CreateController();

            // Act
            var result = await controller.GetById(t.TaskId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ((TaskResponseDto)ok.Value!).TaskId.Should().Be(t.TaskId);
        }

        [Fact]
        public async Task GetById_WhenNotFound_ReturnsNotFound()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskResponseDto?)null);
            var controller = CreateController();

            // Act
            var result = await controller.GetById(Guid.NewGuid());

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            ((NotFoundObjectResult)result).Value.Should().Be("Task not found");
        }

        [Fact]
        public async Task GetById_WhenThrows_Returns500()
        {
            // Arrange
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetByIdAsync(id)).ThrowsAsync(new Exception("boom"));
            var controller = CreateController();

            // Act
            var result = await controller.GetById(id);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var obj = (ObjectResult)result;
            obj.StatusCode.Should().Be(500);
            obj.Value.Should().Be("Failed to get task");
        }

        [Fact]
        public async Task GetByCustomerId_ForwardsFilterAndReturnsOk()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var expected = new List<TaskResponseDto> { MakeTask(customerId: customerId) };
            _serviceMock.Setup(s => s.GetAllAsync(It.Is<TaskFilterDto>(f => f != null && f.CustomerId == customerId)))
                        .ReturnsAsync(expected);

            var controller = CreateController();

            // Act
            var result = await controller.GetByCustomerId(customerId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ((IEnumerable<TaskResponseDto>)ok.Value!).Should().HaveCount(1);
            _serviceMock.Verify(s => s.GetAllAsync(It.Is<TaskFilterDto>(f => f.CustomerId == customerId)), Times.Once);
        }

        [Fact]
        public async Task GetByUserId_ForwardsFilterAndReturnsOk()
        {
            // Arrange
            var userId = "u-123";
            var expected = new List<TaskResponseDto> { MakeTask(userId: userId) };
            _serviceMock.Setup(s => s.GetAllAsync(It.Is<TaskFilterDto>(f => f != null && f.UserId == userId)))
                        .ReturnsAsync(expected);

            var controller = CreateController();

            // Act
            var result = await controller.GetByUserId(userId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ((IEnumerable<TaskResponseDto>)ok.Value!).Should().HaveCount(1);
            _serviceMock.Verify(s => s.GetAllAsync(It.Is<TaskFilterDto>(f => f.UserId == userId)), Times.Once);
        }

        [Fact]
        public async Task GetTotalCount_ReturnsOk()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetTotalCountAsync()).ReturnsAsync(42);
            var controller = CreateController();

            // Act
            var result = await controller.GetTotalCount();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            ((OkObjectResult)result).Value.Should().Be(42);
        }

        [Fact]
        public async Task GetOpenCount_ReturnsOk()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetOpenCountAsync()).ReturnsAsync(7);
            var controller = CreateController();

            // Act
            var result = await controller.GetOpenCount();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            ((OkObjectResult)result).Value.Should().Be(7);
        }

        [Fact]
        public async Task GetRecent_ReturnsOk()
        {
            // Arrange
            var recent = new List<TaskResponseDto> { MakeTask(), MakeTask() };
            _serviceMock.Setup(s => s.GetRecentTasksAsync(10)).ReturnsAsync(recent);
            var controller = CreateController();

            // Act
            var result = await controller.GetRecent(10);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            ((OkObjectResult)result).Value.Should().BeAssignableTo<IEnumerable<TaskResponseDto>>();
            _serviceMock.Verify(s => s.GetRecentTasksAsync(10), Times.Once);
        }

        [Fact]
        public async Task Create_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController(MakeUser());
            controller.ModelState.AddModelError("Title", "Required");

            var dto = new CreateTaskDto { CustomerId = Guid.NewGuid(), UserId = "user-1", Title = "" };

            // Act
            var result = await controller.Create(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Create_Success_ReturnsCreatedAtAction()
        {
            // Arrange
            var performedBy = "user-1";
            var controller = CreateController(MakeUser(performedBy));

            var input = new CreateTaskDto
            {
                CustomerId = Guid.NewGuid(),
                UserId = performedBy,
                Title = "New Task",
                DueDate = DateTime.UtcNow.AddDays(2),
                Priority = Models.Tasks.TaskPriority.High,
                State = Models.Tasks.TaskState.Pending
            };

            var created = MakeTask(id: Guid.NewGuid(), customerId: input.CustomerId, userId: performedBy, title: input.Title);
            _serviceMock.Setup(s => s.CreateAsync(input, performedBy)).ReturnsAsync(created);

            // Act
            var result = await controller.Create(input);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = (CreatedAtActionResult)result;
            createdResult.ActionName.Should().Be(nameof(controller.GetById));
            ((TaskResponseDto)createdResult.Value!).TaskId.Should().Be(created.TaskId);
            _serviceMock.Verify(s => s.CreateAsync(input, performedBy), Times.Once);
        }

        [Fact]
        public async Task Update_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController(MakeUser());
            controller.ModelState.AddModelError("Title", "Required");

            var updateDto = new UpdateTaskDto { Title = null };

            // Act
            var result = await controller.Update(Guid.NewGuid(), updateDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Update_Success_ReturnsOk()
        {
            // Arrange
            var performedBy = "user-1";
            var controller = CreateController(MakeUser(performedBy));

            var id = Guid.NewGuid();
            var updateDto = new UpdateTaskDto { Title = "Updated" };
            var updated = MakeTask(id: id, userId: performedBy, title: "Updated");

            _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, performedBy)).ReturnsAsync(updated);

            // Act
            var result = await controller.Update(id, updateDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ((TaskResponseDto)ok.Value!).TaskId.Should().Be(id);
            _serviceMock.Verify(s => s.UpdateAsync(id, updateDto, performedBy), Times.Once);
        }

        [Fact]
        public async Task Delete_Success_ReturnsNoContent()
        {
            // Arrange
            var performedBy = "user-1";
            var controller = CreateController(MakeUser(performedBy));

            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteAsync(id, performedBy)).Returns(Task.CompletedTask);

            // Act
            var result = await controller.Delete(id);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _serviceMock.Verify(s => s.DeleteAsync(id, performedBy), Times.Once);
        }

        [Fact]
        public async Task Delete_WhenThrows_ReturnsBadRequest()
        {
            // Arrange
            var performedBy = "user-1";
            var controller = CreateController(MakeUser(performedBy));

            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteAsync(id, performedBy)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await controller.Delete(id);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            ((BadRequestObjectResult)result).Value.Should().Be("boom");
        }
    }
}