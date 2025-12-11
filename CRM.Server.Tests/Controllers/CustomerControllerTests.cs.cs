using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRM.Server.Controllers;
using CRM.Server.DTOs;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FluentAssertions;

namespace CRM.Server.Tests.Controllers
{
    public class CustomerControllerTests
    {
        private readonly Mock<ICustomerService> _serviceMock;
        private readonly CustomerController _controller;

        public CustomerControllerTests()
        {
            _serviceMock = new Mock<ICustomerService>();
            _controller = new CustomerController(_serviceMock.Object);
        }

        // --------------------------------------------------------
        // GET ALL
        // --------------------------------------------------------
        [Fact]
        public async Task GetAll_Should_Return_Ok_With_Data()
        {
            // Arrange
            var customers = new List<CustomerResponseDto>
            {
                new CustomerResponseDto
                {
                    CustomerId = Guid.NewGuid(),
                    FirstName = "John",
                    SurName = "Doe",
                    MiddleName = "A",
                    PreferredName = "JD",
                    Email = "john@example.com",
                    Phone = "9876543210",
                    Address = "Hyderabad",
                    CreatedByUserId = Guid.NewGuid(),
                    CreatedAt = "2025-01-01"
                }
            };

            _serviceMock
                .Setup(s => s.FilterAsync(null, null, null, null, null))
                .ReturnsAsync(customers);

            // Act
            var result = await _controller.GetAll(null, null, null, null, null);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            ok.Value.Should().BeEquivalentTo(customers);
        }

        // --------------------------------------------------------
        // GET BY ID
        // --------------------------------------------------------
        [Fact]
        public async Task GetById_Should_Return_Ok_When_Found()
        {
            // Arrange
            var id = Guid.NewGuid();
            var customer = new CustomerResponseDto
            {
                CustomerId = id,
                FirstName = "John",
                SurName = "Doe",
                Email = "john@example.com",
                Phone = "9876543210",
                Address = "Hyderabad",
                CreatedByUserId = Guid.NewGuid(),
                CreatedAt = "2025-01-01"
            };

            _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(customer);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(customer);
        }

        [Fact]
        public async Task GetById_Should_Return_NotFound_When_Not_Found()
        {
            // Arrange
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((CustomerResponseDto?)null);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        // --------------------------------------------------------
        // CREATE
        // --------------------------------------------------------
        [Fact]
        public async Task Create_Should_Return_Ok_With_Response()
        {
            // Arrange
            var dto = new CustomerCreateDto
            {
                FirstName = "John",
                SurName = "Doe",
                MiddleName = "A",
                PreferredName = "JD",
                Email = "john@example.com",
                Phone = "9876543210",
                Address = "Hyderabad",
                CreatedByUserId = Guid.NewGuid()
            };

            var created = new CustomerResponseDto
            {
                CustomerId = Guid.NewGuid(),
                FirstName = dto.FirstName,
                SurName = dto.SurName,
                MiddleName = dto.MiddleName,
                PreferredName = dto.PreferredName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                CreatedByUserId = dto.CreatedByUserId,
                CreatedAt = DateTime.UtcNow.ToString()
            };

            _serviceMock.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            ok.Value.Should().BeEquivalentTo(created);
        }

        // --------------------------------------------------------
        // UPDATE
        // --------------------------------------------------------
        [Fact]
        public async Task Update_Should_Return_NoContent_When_Success()
        {
            var id = Guid.NewGuid();
            var dto = new CustomerUpdateDto
            {
                FirstName = "John",
                SurName = "Doe",
                MiddleName = "A",
                PreferredName = "JD",
                Email = "john@example.com",
                Phone = "9876543210",
                Address = "Hyderabad"
            };

            _serviceMock.Setup(s => s.UpdateAsync(id, dto)).ReturnsAsync(true);

            var result = await _controller.Update(id, dto);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Update_Should_Return_NotFound_When_Failed()
        {
            var id = Guid.NewGuid();
            var dto = new CustomerUpdateDto
            {
                FirstName = "John",
                SurName = "Doe",
                Email = "john@example.com",
                Phone = "9876543210",
                Address = "Hyderabad"
            };

            _serviceMock.Setup(s => s.UpdateAsync(id, dto)).ReturnsAsync(false);

            var result = await _controller.Update(id, dto);

            result.Should().BeOfType<NotFoundResult>();
        }

        // --------------------------------------------------------
        // DELETE
        // --------------------------------------------------------
        [Fact]
        public async Task Delete_Should_Return_NoContent_When_Success()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

            var result = await _controller.Delete(id);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Failed()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteAsync(id)).ReturnsAsync(false);

            var result = await _controller.Delete(id);

            result.Should().BeOfType<NotFoundResult>();
        }
        //[Fact]
        //public async Task Create_Should_Fail_When_Email_Already_Exists()
        //{
        //    // Arrange
        //    var dto = new CustomerCreateDto
        //    {
        //        FirstName = "John",
        //        SurName = "Doe",
        //        MiddleName = "A",
        //        PreferredName = "JD",
        //        Email = "existingemail@example.com",
        //        Phone = "9876543210",
        //        Address = "Hyderabad",
        //        CreatedByUserId = Guid.NewGuid()
        //    };

        //    // ❗ Assume service returns null when email already exists
        //    _serviceMock
        //        .Setup(s => s.CreateAsync(dto))
        //        .ReturnsAsync((CustomerResponseDto?)null);

        //    // Act
        //    var result = await _controller.Create(dto);

        //    // ❗ WRONG EXPECTATION ON PURPOSE → THIS WILL FAIL
        //    // The controller actually returns Ok(null) OR should ideally return BadRequest.
        //    // But we expect a valid customer to be returned.
        //    result.Should().BeOfType<OkObjectResult>("we expect customer creation to succeed");

        //    var ok = result as OkObjectResult;
        //    ok!.Value.Should().NotBeNull("we expected a valid created customer");
        }

    }

