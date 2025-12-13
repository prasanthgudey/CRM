using CRM.Server.Common.Paging;
using CRM.Server.DTOs;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/customers")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _service;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ICustomerService service, ILogger<CustomerController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =============================================
        // GET ALL (FILTER)
        // =============================================
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? name,
            [FromQuery] string? email,
            [FromQuery] string? phone,
            [FromQuery] string? address,
            [FromQuery] string? search)
        {
            _logger.LogInformation("GetAll customers called");
            var result = await _service.FilterAsync(name, email, phone, address, search);
            return Ok(result);
        }

        // =============================================
        // GET BY ID
        // =============================================
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("Get customer by ID: {CustomerId}", id);

            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        // =============================================
        // CREATE CUSTOMER
        // =============================================
        [HttpPost]
        public async Task<IActionResult> Create(CustomerCreateDto dto)
        {
            _logger.LogInformation("Create customer called");

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            dto.CreatedByUserId = performedBy;

            var result = await _service.CreateAsync(dto, performedBy!);
            return Ok(result);
        }

        // =============================================
        // UPDATE CUSTOMER
        // =============================================
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, CustomerUpdateDto dto)
        {
            _logger.LogInformation("Update customer {CustomerId}", id);

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var updated = await _service.UpdateAsync(id, dto, performedBy!);

            return updated ? NoContent() : NotFound();
        }

        // =============================================
        // DELETE CUSTOMER
        // =============================================
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("Delete customer {CustomerId}", id);

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var deleted = await _service.DeleteAsync(id, performedBy!);

            return deleted ? NoContent() : NotFound();
        }

        // =============================================
        // PAGED LIST
        // =============================================
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] PageParams parms)
        {
            _logger.LogInformation("GetPaged customers called");
            var result = await _service.GetPagedAsync(parms);
            return Ok(result);
        }

        // =============================================
        // DASHBOARD: TOTAL COUNT
        // =============================================
        [HttpGet("count")]
        public async Task<IActionResult> GetTotalCount()
        {
            _logger.LogInformation("Get total customers count");

            var count = await _service.GetTotalCountAsync();
            return Ok(count);
        }

        // =============================================
        // DASHBOARD: NEW CUSTOMERS IN X DAYS
        // =============================================
        [HttpGet("new")]
        public async Task<IActionResult> GetNewCustomers([FromQuery] int days = 7)
        {
            _logger.LogInformation("Get new customers in last {Days} days", days);

            var count = await _service.GetNewCustomersCountAsync(days);
            return Ok(count);
        }
    }
}
