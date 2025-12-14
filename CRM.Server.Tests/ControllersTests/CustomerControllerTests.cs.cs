using CRM.Server.Controllers;
using CRM.Server.DTOs;

using CRM.Server.Services.Interfaces;
using CRM.Server.Common.Paging;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CRM.Server.Tests.Controllers
{
    public class CustomerControllerTests
    {
        private readonly Mock<ICustomerService> _serviceMock;
        private readonly Mock<ILogger<CustomerController>> _loggerMock;
        private readonly CustomerController _controller;

        public CustomerControllerTests()
        {
            _serviceMock = new Mock<ICustomerService>();
            _loggerMock = new Mock<ILogger<CustomerController>>();

            _controller = new CustomerController(
                _serviceMock.Object,
                _loggerMock.Object
            );

            SetFakeUser();
        }

        // =============================================
        // GET ALL
        // =============================================
        [Fact]
        
        public async Task GetAll_ReturnsOkResult()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.FilterAsync(
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(new List<CustomerResponseDto>());

            // Act
            var result = await _controller.GetAll(null, null, null, null, null);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }


        // =============================================
        // GET BY ID
        // =============================================
        [Fact]
        public async Task GetById_WhenFound_ReturnsOk()
        {
            var id = Guid.NewGuid();

            _serviceMock.Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync(new CustomerResponseDto { CustomerId = id });

            var result = await _controller.GetById(id);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
       
        public async Task GetById_WhenNotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();

            _serviceMock
                .Setup(s => s.GetByIdAsync(id))
                .Returns(Task.FromResult<CustomerResponseDto?>(null));

            var result = await _controller.GetById(id);

            result.Should().BeOfType<NotFoundResult>();
        }


        // =============================================
        // CREATE
        // =============================================
        
        [Fact]
        public async Task Create_ReturnsOk()
        {
            
            var dto = new CustomerCreateDto
            {
                PreferredName = "Test Customer"
            };

            _serviceMock
                .Setup(s => s.CreateAsync(
                    It.IsAny<CustomerCreateDto>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new CustomerResponseDto());

            // Act
            var result = await _controller.Create(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // UPDATE
        // =============================================
        [Fact]
        public async Task Update_WhenSuccess_ReturnsNoContent()
        {
            var id = Guid.NewGuid();
            var dto = new CustomerUpdateDto();

            _serviceMock.Setup(s =>
                s.UpdateAsync(id, dto, It.IsAny<string>()))
                .ReturnsAsync(true);

            var result = await _controller.Update(id, dto);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Update_WhenNotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            var dto = new CustomerUpdateDto();

            _serviceMock.Setup(s =>
                s.UpdateAsync(id, dto, It.IsAny<string>()))
                .ReturnsAsync(false);

            var result = await _controller.Update(id, dto);

            result.Should().BeOfType<NotFoundResult>();
        }

        // =============================================
        // DELETE
        // =============================================
        [Fact]
        public async Task Delete_WhenSuccess_ReturnsNoContent()
        {
            var id = Guid.NewGuid();

            _serviceMock.Setup(s =>
                s.DeleteAsync(id, It.IsAny<string>()))
                .ReturnsAsync(true);

            var result = await _controller.Delete(id);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_WhenNotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();

            _serviceMock.Setup(s =>
                s.DeleteAsync(id, It.IsAny<string>()))
                .ReturnsAsync(false);

            var result = await _controller.Delete(id);

            result.Should().BeOfType<NotFoundResult>();
        }

        // =============================================
        // PAGED
        // =============================================
        [Fact]
        public async Task GetPaged_ReturnsOk()
        {
            var pageParams = new PageParams
            {
                Page = 1,
                PageSize = 10
            };

            _serviceMock
                .Setup(s => s.GetPagedAsync(It.IsAny<PageParams>()))
                .ReturnsAsync(new PagedResult<CustomerResponseDto>());

            var result = await _controller.GetPaged(pageParams);

            result.Should().BeOfType<OkObjectResult>();
        }



        // =============================================
        // COUNT
        // =============================================
        [Fact]
        public async Task GetCount_ReturnsOk()
        {
            _serviceMock.Setup(s => s.GetTotalCountAsync())
                .ReturnsAsync(10);

            var result = await _controller.GetCount();

            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // NEW CUSTOMERS
        // =============================================
        [Fact]
        public async Task GetNew_ReturnsOk()
        {
            _serviceMock.Setup(s =>
                s.GetNewCustomersCountAsync(7))
                .ReturnsAsync(3);

            var result = await _controller.GetNew(7);

            result.Should().BeOfType<OkObjectResult>();
        }

        // =============================================
        // HELPER
        // =============================================
        private void SetFakeUser()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };
        }
    }
}
