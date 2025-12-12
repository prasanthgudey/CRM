using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CRM.Server.Common.Paging;
using CRM.Server.Controllers;
using CRM.Server.DTOs;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CRM.Tests.Controllers
{
    public class CustomerControllerTests
    {
        private readonly Mock<ICustomerService> _serviceMock;
        private readonly CustomerController _controller;

        public CustomerControllerTests()
        {
            _serviceMock = new Mock<ICustomerService>();

            _controller = new CustomerController(_serviceMock.Object);

            // Fake HttpContext (needed for Create/Update/Delete)
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "actor-1")
                        })
                    )
                }
            };
        }

        // ---------------------------------------------------------
        // 1) GET /api/customers (Filter)
        // ---------------------------------------------------------
        [Fact]
        public async Task GetAll_ReturnsOk_WithServiceResult()
        {
            var customers = new List<CustomerResponseDto>
            {
                new CustomerResponseDto { CustomerId = Guid.NewGuid(), FirstName = "Ali" }
            };

            _serviceMock.Setup(s => s.FilterAsync("a", null, null, null, null))
                .ReturnsAsync(customers);

            var result = await _controller.GetAll("a", null, null, null, null)
                            as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(customers, result.Value);
        }

        // ---------------------------------------------------------
        // 2) GET /api/customers/{id}
        // ---------------------------------------------------------
        [Fact]
        public async Task GetById_WhenFound_ReturnsOk()
        {
            var id = Guid.NewGuid();
            var dto = new CustomerResponseDto { CustomerId = id, FirstName = "Ali" };

            _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(dto);

            var result = await _controller.GetById(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(dto, result.Value);
        }

        [Fact]
        public async Task GetById_WhenNotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();

            _serviceMock.Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync((CustomerResponseDto?)null);

            var result = await _controller.GetById(id);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------------------------------------------------------
        // 3) POST /api/customers
        // ---------------------------------------------------------
        [Fact]
        public async Task Create_ReturnsOk_WithCreatedCustomer()
        {
            var dto = new CustomerCreateDto
            {
                FirstName = "New",
                Email = "x@a.com"
            };

            var response = new CustomerResponseDto
            {
                CustomerId = Guid.NewGuid(),
                FirstName = "New"
            };

            _serviceMock.Setup(s => s.CreateAsync(dto, "actor-1"))
                .ReturnsAsync(response);

            var result = await _controller.Create(dto) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(response, result.Value);
            Assert.Equal("actor-1", dto.CreatedByUserId);
        }

        // ---------------------------------------------------------
        // 4) PUT /api/customers/{id}
        // ---------------------------------------------------------
        [Fact]
        public async Task Update_WhenFound_ReturnsNoContent()
        {
            var id = Guid.NewGuid();
            var dto = new CustomerUpdateDto { FirstName = "Updated" };

            _serviceMock.Setup(s => s.UpdateAsync(id, dto, "actor-1"))
                .ReturnsAsync(true);

            var result = await _controller.Update(id, dto);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Update_WhenNotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            var dto = new CustomerUpdateDto { FirstName = "Updated" };

            _serviceMock.Setup(s => s.UpdateAsync(id, dto, "actor-1"))
                .ReturnsAsync(false);

            var result = await _controller.Update(id, dto);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------------------------------------------------------
        // 5) DELETE /api/customers/{id}
        // ---------------------------------------------------------
        [Fact]
        public async Task Delete_WhenFound_ReturnsNoContent()
        {
            var id = Guid.NewGuid();

            _serviceMock.Setup(s => s.DeleteAsync(id, "actor-1"))
                .ReturnsAsync(true);

            var result = await _controller.Delete(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_WhenNotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();

            _serviceMock.Setup(s => s.DeleteAsync(id, "actor-1"))
                .ReturnsAsync(false);

            var result = await _controller.Delete(id);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------------------------------------------------------
        // 6) GET /api/customers/paged
        // ---------------------------------------------------------
        [Fact]
        public async Task GetPaged_ReturnsOk()
        {
            var parms = new PageParams { Page = 1, PageSize = 10 };

            var pagedResult = new PagedResult<CustomerResponseDto>
            {
                Items = new List<CustomerResponseDto>
                {
                    new CustomerResponseDto { FirstName = "X" }
                },
                Page = 1,
                PageSize = 10,
                TotalCount = 1
            };

            _serviceMock.Setup(s => s.GetPagedAsync(parms))
                .ReturnsAsync(pagedResult);

            var result = await _controller.GetPaged(parms) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(pagedResult, result.Value);
        }

        // ---------------------------------------------------------
        // 7) GET /api/customers/count
        // ---------------------------------------------------------
        [Fact]
        public async Task GetTotalCount_ReturnsOk()
        {
            _serviceMock.Setup(s => s.GetTotalCountAsync())
                .ReturnsAsync(25);

            var result = await _controller.GetTotalCount() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(25, result.Value);
        }

        // ---------------------------------------------------------
        // 8) GET /api/customers/new?days=x
        // ---------------------------------------------------------
        [Fact]
        public async Task GetNewCustomers_ReturnsOk()
        {
            _serviceMock.Setup(s => s.GetNewCustomersCountAsync(7))
                .ReturnsAsync(5);

            var result = await _controller.GetNewCustomers(7) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(5, result.Value);
        }
    }
}
