using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Server.Common.Paging;
using CRM.Server.DTOs;
using CRM.Server.Models;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Services;
using CRM.Server.Services.Interfaces;
using Moq;
using Xunit;

namespace CRM.Tests.Services
{
    public class CustomerServiceTests
    {
        private readonly Mock<ICustomerRepository> _repoMock;
        private readonly Mock<IAuditLogService> _auditMock;
        private readonly CustomerService _service;

        public CustomerServiceTests()
        {
            _repoMock = new Mock<ICustomerRepository>();
            _auditMock = new Mock<IAuditLogService>();
            _service = new CustomerService(_repoMock.Object, _auditMock.Object);
        }

        // Helper to build a sample customer
        private Customer BuildCustomer(
            Guid? id = null,
            string firstName = "John",
            string surName = "Doe",
            string? createdAt = null,
            string? email = "john@example.com",
            string? phone = "12345",
            string? address = "Street 1",
            string? createdBy = "creator")
        {
            return new Customer
            {
                CustomerId = id ?? Guid.NewGuid(),
                FirstName = firstName,
                SurName = surName,
                MiddleName = null,
                PreferredName = null,
                Email = email,
                Phone = phone,
                Address = address,
                CreatedByUserId = createdBy,
                CreatedAt = createdAt ?? DateTime.UtcNow.ToString("O")
            };
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedDtos()
        {
            // Arrange
            var list = new List<Customer>
            {
                BuildCustomer(firstName: "A"),
                BuildCustomer(firstName: "B")
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(list);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.FirstName == "A");
            Assert.Contains(result, r => r.FirstName == "B");
        }

        [Fact]
        public async Task GetByIdAsync_WhenExists_ReturnsDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var cust = BuildCustomer(id: id, firstName: "X");
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(cust);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result!.CustomerId);
            Assert.Equal("X", result.FirstName);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Customer?)null);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateCustomer_AndCallAudit()
        {
            // Arrange
            var dto = new CustomerCreateDto
            {
                FirstName = "New",
                SurName = "Customer",
                MiddleName = null,
                PreferredName = null,
                Email = "new@example.com",
                Phone = "999",
                Address = "Addr",
                CreatedByUserId = "creator-id"
            };

            // Repository returns the created entity (service doesn't rely on return value but interface requires it)
            _repoMock
                .Setup(r => r.CreateAsync(It.IsAny<Customer>()))
                .ReturnsAsync((Customer c) => c);

            // Act
            var performer = "actor-1";
            var createdDto = await _service.CreateAsync(dto, performer);

            // Assert repo called
            _repoMock.Verify(r => r.CreateAsync(It.Is<Customer>(c =>
                c.FirstName == dto.FirstName &&
                c.Email == dto.Email &&
                c.CreatedByUserId == dto.CreatedByUserId
            )), Times.Once);

            // Assert result mapped correctly
            Assert.Equal(dto.FirstName, createdDto.FirstName);
            Assert.Equal(dto.Email, createdDto.Email);

            // Audit called
            _auditMock.Verify(a => a.LogAsync(
                performer,
                It.IsAny<string>(),
                "Customer Created",
                "Customer",
                true,
                null,
                null,
                It.Is<string>(s => s.Contains(dto.FirstName) && s.Contains(dto.Email))
            ), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenAuditThrows_ShouldNotBubbleException()
        {
            // Arrange
            var dto = new CustomerCreateDto { FirstName = "X", SurName = "Y", CreatedByUserId = "c" };
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Customer>())).ReturnsAsync((Customer c) => c);

            _auditMock
                .Setup(a => a.LogAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ThrowsAsync(new Exception("audit failure"));

            // Act & Assert: should not throw
            var ex = await Record.ExceptionAsync(() => _service.CreateAsync(dto, "p"));
            Assert.Null(ex);

            // Repo still called
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Customer>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenNotFound_ReturnsFalse_AndNoAudit()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Customer?)null);

            // Act
            var result = await _service.UpdateAsync(id, new CustomerUpdateDto { FirstName = "A" }, "actor");

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Customer>()), Times.Never);
            _auditMock.Verify(a => a.LogAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenFound_UpdatesAndAudits_ReturnsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            var existing = BuildCustomer(id: id, firstName: "Old");
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);

            var dto = new CustomerUpdateDto
            {
                FirstName = "New",
                SurName = "NewSur",
                MiddleName = null,
                PreferredName = null,
                Email = "u@e.com",
                Phone = "111",
                Address = "addr"
            };

            // Act
            var res = await _service.UpdateAsync(id, dto, "actor-2");

            // Assert
            Assert.True(res);
            _repoMock.Verify(r => r.UpdateAsync(It.Is<Customer>(c =>
                c.FirstName == dto.FirstName &&
                c.Email == dto.Email
            )), Times.Once);

            _auditMock.Verify(a => a.LogAsync(
                    "actor-2",                     // performedByUserId
                    id.ToString(),                // targetUserId
                    "Customer Updated",           // action
                    "Customer",                   // entityName
                     true,                         // isSuccess
                    null,                         // ipAddress
                    It.Is<string>(oldVal => oldVal.Contains("Old")),   // oldValue
                    It.Is<string>(newVal => newVal.Contains("New") && newVal.Contains("u@e.com")) // newValue
                   ), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenNotFound_ReturnsFalse_AndNoAudit()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Customer?)null);

            // Act
            var res = await _service.DeleteAsync(id, "actor");

            // Assert
            Assert.False(res);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Customer>()), Times.Never);
            _auditMock.Verify(a => a.LogAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenFound_DeletesAndAudits_ReturnsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            var existing = BuildCustomer(id: id, firstName: "DeleteMe");
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _repoMock.Setup(r => r.DeleteAsync(existing)).Returns(Task.CompletedTask);

            // Act
            var res = await _service.DeleteAsync(id, "deleter");

            // Assert
            Assert.True(res);
            _repoMock.Verify(r => r.DeleteAsync(It.Is<Customer>(c => c.CustomerId == id)), Times.Once);

            _auditMock.Verify(a => a.LogAsync(
                     "deleter",                        // performedByUserId
                     id.ToString(),                    // targetUserId
                     "Customer Deleted",               // action
                     "Customer",                       // entityName
                     true,                             // isSuccess  <-- REQUIRED
                  null,                             // ipAddress
                  It.Is<string>(oldVal =>
             oldVal.Contains("DeleteMe")   // oldValue
         ),
     null                               // newValue
 ), Times.Once);
        }


            [Fact]
        public async Task FilterAsync_ByName_FiltersCorrectly()
        {
            // Arrange
            var c1 = BuildCustomer(firstName: "Alice", surName: "Smith");
            var c2 = BuildCustomer(firstName: "Bob", surName: "Jones");
            var c3 = BuildCustomer(firstName: "Alicia", surName: "Brown");
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Customer> { c1, c2, c3 });

            // Act
            var result = await _service.FilterAsync(name: "Ali", email: null, phone: null, address: null, search: null);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, r =>
                Assert.True(
                    (r.FirstName?.Contains("Ali", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.SurName?.Contains("Ali", StringComparison.OrdinalIgnoreCase) ?? false)
                )
            );
        }

        [Fact]
        public async Task FilterAsync_ByEmail_Phone_Address_Search_AllWork()
        {
            // Arrange
            var c1 = BuildCustomer(firstName: "X", email: "test@domain.com", phone: "9999", address: "New York");
            var c2 = BuildCustomer(firstName: "Y", email: "other@domain.com", phone: "8888", address: "LA");
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Customer> { c1, c2 });

            // filter by email
            var byEmail = await _service.FilterAsync(name: null, email: "test@", phone: null, address: null, search: null);
            Assert.Single(byEmail);
            Assert.Equal("test@domain.com", byEmail[0].Email);

            // filter by phone
            var byPhone = await _service.FilterAsync(name: null, email: null, phone: "8888", address: null, search: null);
            Assert.Single(byPhone);
            Assert.Equal("8888", byPhone[0].Phone);

            // filter by address
            var byAddress = await _service.FilterAsync(name: null, email: null, phone: null, address: "la", search: null);
            Assert.Single(byAddress);
            Assert.Equal("LA", byAddress[0].Address);

            // search across fields (search for domain)
            var bySearch = await _service.FilterAsync(name: null, email: null, phone: null, address: null, search: "other@");
            Assert.Single(bySearch);
            Assert.Equal("other@domain.com", bySearch[0].Email);
        }

        [Fact]
        public async Task GetTotalCountAsync_ReturnsCountOrZero()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Customer> { BuildCustomer(), BuildCustomer() });

            // Act
            var count = await _service.GetTotalCountAsync();

            // Assert
            Assert.Equal(2, count);

            // Null case
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync((List<Customer>?)null);
            var count2 = await _service.GetTotalCountAsync();
            Assert.Equal(0, count2);
        }

        [Fact]
        public async Task GetNewCustomersCountAsync_CountsOnlyRecent()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var recent = BuildCustomer(createdAt: now.ToString("O")); // within cutoff
            var old = BuildCustomer(createdAt: now.AddDays(-10).ToString("O")); // outside cutoff
            var unparsable = BuildCustomer(createdAt: "not a date"); // should be ignored

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Customer> { recent, old, unparsable });

            // Act
            var count7 = await _service.GetNewCustomersCountAsync(7); // cutoff: now - 7 days

            // Assert
            Assert.Equal(1, count7);

            // days = 30 should include both recent and old
            var count30 = await _service.GetNewCustomersCountAsync(30);
            Assert.Equal(2, count30);
        }

        [Fact]
        public async Task GetNewCustomersCountAsync_WhenNoList_ReturnsZero()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync((List<Customer>?)null);

            // Act
            var count = await _service.GetNewCustomersCountAsync(7);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task GetPagedAsync_MapsPagedResult()
        {
            // Arrange
            var customers = new List<Customer>
            {
                BuildCustomer(firstName: "P1"),
                BuildCustomer(firstName: "P2")
            };

            var pagedIn = new PagedResult<Customer>
            {
                Items = customers,
                Page = 1,
                PageSize = 10,
                TotalCount = 2
            };

            _repoMock.Setup(r => r.GetPagedAsync(It.IsAny<PageParams>())).ReturnsAsync(pagedIn);

            // Act
            var pagedOut = await _service.GetPagedAsync(new PageParams { Page = 1, PageSize = 10 });

            // Assert
            Assert.Equal(2, pagedOut.Items.Count);
            Assert.Equal(pagedIn.Page, pagedOut.Page);
            Assert.Equal(pagedIn.PageSize, pagedOut.PageSize);
            Assert.Equal(pagedIn.TotalCount, pagedOut.TotalCount);
            Assert.Contains(pagedOut.Items, it => it.FirstName == "P1");
        }

        [Fact]
        public async Task SafeAudit_SwallowExceptions_DoesNotThrowDuringUpdateOrDelete()
        {
            // Arrange - make audit throw
            _auditMock
                .Setup(a => a.LogAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ThrowsAsync(new Exception("boom"));

            // Setup repo for update
            var id = Guid.NewGuid();
            var existing = BuildCustomer(id: id, firstName: "S");
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);

            // Act - Update should not throw even if audit fails
            var updateRes = await _service.UpdateAsync(id, new CustomerUpdateDto { FirstName = "X" }, "actor");
            Assert.True(updateRes);

            // Setup repo for delete
            var id2 = Guid.NewGuid();
            var existing2 = BuildCustomer(id: id2, firstName: "D");
            _repoMock.Setup(r => r.GetByIdAsync(id2)).ReturnsAsync(existing2);
            _repoMock.Setup(r => r.DeleteAsync(existing2)).Returns(Task.CompletedTask);

            // Act - Delete should not throw even if audit fails
            var deleteRes = await _service.DeleteAsync(id2, "deleter");
            Assert.True(deleteRes);
        }

        [Fact]
        public async Task ExistsAsync_IsNotCalledDirectlyByService_ButRepoHasMethod()
        {
            // This test simply ensures the repository method exists in the mock setup contract.
            // The service currently doesn't call ExistsAsync; we still ensure mockable.
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);

            var exists = await _repoMock.Object.ExistsAsync(Guid.NewGuid());
            Assert.True(exists);
            _repoMock.Verify(r => r.ExistsAsync(It.IsAny<Guid>()), Times.Once);
        }
    }
}
