using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Server.DTOs;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CRM.Server.Tests.Services
{
    public class CustomerServiceTests
    {
        private readonly Mock<ICustomerRepository> _repoMock;
        private readonly CustomerService _service;

        public CustomerServiceTests()
        {
            _repoMock = new Mock<ICustomerRepository>();
            _service = new CustomerService(_repoMock.Object);
        }

        // --------------------------------------------------------
        // GET BY ID
        // --------------------------------------------------------
        [Fact]
        public async Task GetByIdAsync_Should_Return_Customer_When_Found()
        {
            // Arrange
            var id = Guid.NewGuid();
            var customer = new Customer
            {
                CustomerId = id,
                FirstName = "John",
                SurName = "Doe",
                MiddleName = "A",
                PreferredName = "JD",
                Email = "john@example.com",
                Phone = "98765",
                Address = "Hyderabad",
                CreatedByUserId = Guid.NewGuid(),
                CreatedAt = "2025-01-01"
            };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(customer);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            result.Should().NotBeNull();
            result!.CustomerId.Should().Be(id);
            result.FirstName.Should().Be("John");
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Null_When_Not_Found()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Customer?)null);

            var result = await _service.GetByIdAsync(id);

            result.Should().BeNull();
        }

        // --------------------------------------------------------
        // CREATE
        // --------------------------------------------------------
        [Fact]
        public async Task CreateAsync_Should_Save_And_Return_CustomerResponseDto()
        {
            var dto = new CustomerCreateDto
            {
                FirstName = "John",
                SurName = "Doe",
                Email = "john@example.com",
                Phone = "98765",
                Address = "Hyd",
                CreatedByUserId = Guid.NewGuid()
            };

            Customer? savedCustomer = null;

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Customer>()))
                .Callback<Customer>(c => savedCustomer = c)
                .ReturnsAsync((Customer c) => c);   // ✔ Return correct type



            var result = await _service.CreateAsync(dto);

            // Assert saved customer to DB
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Customer>()), Times.Once);
            savedCustomer.Should().NotBeNull();
            savedCustomer!.FirstName.Should().Be(dto.FirstName);

            // Assert returned DTO
            result.Should().NotBeNull();
            result.FirstName.Should().Be(dto.FirstName);
        }

        // --------------------------------------------------------
        // UPDATE
        // --------------------------------------------------------
        [Fact]
        public async Task UpdateAsync_Should_Return_True_When_Updated()
        {
            var id = Guid.NewGuid();
            var existing = new Customer
            {
                CustomerId = id,
                FirstName = "Old"
            };

            var dto = new CustomerUpdateDto
            {
                FirstName = "New",
                SurName = "Updated",
                Email = "new@mail.com",
                Phone = "1111",
                Address = "New Address"
            };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);

            var result = await _service.UpdateAsync(id, dto);

            result.Should().BeTrue();
            existing.FirstName.Should().Be(dto.FirstName);

            _repoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Return_False_When_Not_Found()
        {
            var id = Guid.NewGuid();

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Customer?)null);

            var dto = new CustomerUpdateDto
            {
                FirstName = "New",
                SurName = "Updated",
                Email = "a@a.com",
                Phone = "111",
                Address = "Hyd"
            };

            var result = await _service.UpdateAsync(id, dto);

            result.Should().BeFalse();
        }

        // --------------------------------------------------------
        // DELETE
        // --------------------------------------------------------
        [Fact]
        public async Task DeleteAsync_Should_Return_True_When_Deleted()
        {
            var id = Guid.NewGuid();
            var customer = new Customer { CustomerId = id };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(customer);

            var result = await _service.DeleteAsync(id);

            result.Should().BeTrue();
            _repoMock.Verify(r => r.DeleteAsync(customer), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Should_Return_False_When_Not_Found()
        {
            var id = Guid.NewGuid();

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Customer?)null);

            var result = await _service.DeleteAsync(id);

            result.Should().BeFalse();
        }

        // --------------------------------------------------------
        // FILTER
        // --------------------------------------------------------
        [Fact]
        public async Task FilterAsync_Should_Filter_By_Name()
        {
            var data = new List<Customer>
            {
                new Customer { FirstName = "John", SurName="Doe", Email="a@a.com" },
                new Customer { FirstName = "Jane", SurName="Smith", Email="b@b.com" }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(data);

            var result = await _service.FilterAsync("John", null, null, null, null);

            result.Should().HaveCount(1);
            result.First().FirstName.Should().Be("John");
        }

        // --------------------------------------------------------
        // MEANINGFUL FAILING TEST (for presentation)
        // --------------------------------------------------------
        [Fact]
        public async Task CreateAsync_Should_Fail_When_Email_Is_Missing()
        {
            // Arrange
            var dto = new CustomerCreateDto
            {
                FirstName = "Test",
                SurName = "User",
                Email = "",    // ❌ INVALID ON PURPOSE
                Phone = "1111",
                Address = "Hyd",
                CreatedByUserId = Guid.NewGuid()
            };

            // Act
            Func<Task> act = async () => await _service.CreateAsync(dto);

            // ❗ Expected FAIL (service does NOT validate email)
            await act.Should().ThrowAsync<Exception>(
                "Email must be validated — failing test to show missing validation"
            );
        }
    }
}
