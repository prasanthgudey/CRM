using System;
using System.Threading.Tasks;
using CRM.Server.Data;
using CRM.Server.Models;
using CRM.Server.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRM.Server.Tests.Repositories
{
    public class CustomerRepositoryTests
    {
        private ApplicationDbContext CreateDb()
        {
            return new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);
        }

        // -------------------------------------------------------
        // CREATE
        // -------------------------------------------------------
        [Fact]
        public async Task CreateAsync_ShouldAddCustomer()
        {
            var db = CreateDb();
            var repo = new CustomerRepository(db);

            var customer = new Customer
            {
                CustomerId = Guid.NewGuid(),
                FirstName = "Raj",
                SurName = "Kumar",
                Email = "raj@example.com",
                Phone = "12345",
                Address = "India",
                CreatedByUserId = "admin1",
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            await repo.CreateAsync(customer);

            var saved = await db.Customers.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("Raj", saved.FirstName);
        }

        // -------------------------------------------------------
        // GET ALL
        // -------------------------------------------------------
        [Fact]
        public async Task GetAllAsync_ShouldReturnAllCustomers()
        {
            var db = CreateDb();
            var repo = new CustomerRepository(db);

            await db.Customers.AddAsync(new Customer
            {
                CustomerId = Guid.NewGuid(),
                FirstName = "A",
                SurName = "Test",
                Email = "a@test.com",
                Phone = "111",
                Address = "X",
                CreatedByUserId = "admin",
                CreatedAt = DateTime.UtcNow.ToString("O")
            });

            await db.Customers.AddAsync(new Customer
            {
                CustomerId = Guid.NewGuid(),
                FirstName = "B",
                SurName = "Test",
                Email = "b@test.com",
                Phone = "222",
                Address = "Y",
                CreatedByUserId = "admin",
                CreatedAt = DateTime.UtcNow.ToString("O")
            });

            await db.SaveChangesAsync();

            var result = await repo.GetAllAsync();

            Assert.Equal(2, result.Count);
        }

        // -------------------------------------------------------
        // GET BY ID
        // -------------------------------------------------------
        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectCustomer()
        {
            var db = CreateDb();
            var repo = new CustomerRepository(db);

            var id = Guid.NewGuid();

            var customer = new Customer
            {
                CustomerId = id,
                FirstName = "John",
                SurName = "Smith",
                Email = "john@test.com",
                Phone = "999",
                Address = "Hyderabad",
                CreatedByUserId = "admin",
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            await db.Customers.AddAsync(customer);
            await db.SaveChangesAsync();

            var result = await repo.GetByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal("John", result!.FirstName);
        }

        // -------------------------------------------------------
        // UPDATE
        // -------------------------------------------------------
        [Fact]
        public async Task UpdateAsync_ShouldModifyCustomer()
        {
            var db = CreateDb();
            var repo = new CustomerRepository(db);

            var customer = new Customer
            {
                CustomerId = Guid.NewGuid(),
                FirstName = "Old",
                SurName = "Name",
                Email = "old@test.com",
                Phone = "000",
                Address = "Old Address",
                CreatedByUserId = "admin",
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            await db.Customers.AddAsync(customer);
            await db.SaveChangesAsync();

            // Update values
            customer.FirstName = "New";
            customer.Address = "New Address";

            await repo.UpdateAsync(customer);

            var updated = await db.Customers.FirstAsync();

            Assert.Equal("New", updated.FirstName);
            Assert.Equal("New Address", updated.Address);
        }

        // -------------------------------------------------------
        // DELETE
        // -------------------------------------------------------
        [Fact]
        public async Task DeleteAsync_ShouldRemoveCustomer()
        {
            var db = CreateDb();
            var repo = new CustomerRepository(db);

            var customer = new Customer
            {
                CustomerId = Guid.NewGuid(),
                FirstName = "Delete",
                SurName = "Me",
                Email = "delete@test.com",
                Phone = "123",
                Address = "Somewhere",
                CreatedByUserId = "admin",
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            await db.Customers.AddAsync(customer);
            await db.SaveChangesAsync();

            await repo.DeleteAsync(customer);

            Assert.Equal(0, await db.Customers.CountAsync());
        }

        // -------------------------------------------------------
        // EXISTS
        // -------------------------------------------------------
        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_IfExists()
        {
            var db = CreateDb();
            var repo = new CustomerRepository(db);

            var id = Guid.NewGuid();

            await db.Customers.AddAsync(new Customer
            {
                CustomerId = id,
                FirstName = "Exists",
                SurName = "Test",
                Email = "exists@test.com",
                Phone = "123",
                Address = "ABC",
                CreatedByUserId = "admin",
                CreatedAt = DateTime.UtcNow.ToString("O")
            });

            await db.SaveChangesAsync();

            var exists = await repo.ExistsAsync(id);

            Assert.True(exists);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_IfNotExists()
        {
            var db = CreateDb();
            var repo = new CustomerRepository(db);

            var exists = await repo.ExistsAsync(Guid.NewGuid());

            Assert.False(exists);
        }
    }
}
