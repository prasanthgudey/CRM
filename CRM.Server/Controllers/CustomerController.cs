using CRM.Server.Common.Paging;
using CRM.Server.DTOs;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/customers")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _service;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            ICustomerService service,
            ILogger<CustomerController> logger)
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
            return Ok(await _service.FilterAsync(name, email, phone, address, search));
        }

        // =============================================
        // GET BY ID
        // =============================================
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var customer = await _service.GetByIdAsync(id);
            return customer == null ? NotFound() : Ok(customer);
        }

        // =============================================
        // CREATE
        // =============================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentException("Customer data is required");

            var performedBy = GetUserId();

            var result = await _service.CreateAsync(dto, performedBy);
            return Ok(result);
        }

        // =============================================
        // UPDATE
        // =============================================
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CustomerUpdateDto dto)
        {
            if (dto == null)
                throw new ArgumentException("Update data is required");

            var performedBy = GetUserId();

            var updated = await _service.UpdateAsync(id, dto, performedBy);
            return updated ? NoContent() : NotFound();
        }

        // =============================================
        // DELETE
        // =============================================
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var performedBy = GetUserId();

            var deleted = await _service.DeleteAsync(id, performedBy);
            return deleted ? NoContent() : NotFound();
        }

        // =============================================
        // PAGED
        // =============================================
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] PageParams parms)
        {
            return Ok(await _service.GetPagedAsync(parms));
        }

        // =============================================
        // DASHBOARD: TOTAL COUNT
        // =============================================
        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            return Ok(await _service.GetTotalCountAsync());
        }

        // =============================================
        // DASHBOARD: NEW CUSTOMERS
        // =============================================
        [HttpGet("new")]
        public async Task<IActionResult> GetNew([FromQuery] int days = 7)
        {
            if (days <= 0)
                throw new ArgumentException("Days must be greater than zero");

            return Ok(await _service.GetNewCustomersCountAsync(days));
        }

        // =============================================
        // HELPER
        // =============================================
        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? throw new InvalidOperationException("User identity not found");
        }
    }
}
