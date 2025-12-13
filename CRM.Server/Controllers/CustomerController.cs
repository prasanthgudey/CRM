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

        public CustomerController(ICustomerService service)
        {
            _service = service;
        }

        // ============================================
        // GET ALL + FILTER
        // ============================================
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? name,
            [FromQuery] string? email,
            [FromQuery] string? phone,
            [FromQuery] string? address,
            [FromQuery] string? search)
        {
            var result = await _service.FilterAsync(name, email, phone, address, search);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        // ============================================
        // CREATE CUSTOMER
        // ============================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid customer data.");

            // Load all customers for duplicate checks
            var all = await _service.FilterAsync(null, null, null, null, null);

            var normalizedEmail = dto.Email?.Trim().ToLower();
            var normalizedPhone = dto.Phone?.Trim();

            if (!string.IsNullOrWhiteSpace(normalizedEmail) &&
                all.Any(c => c.Email != null &&
                             c.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict("A customer with this email already exists.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedPhone) &&
                all.Any(c => c.Phone == normalizedPhone))
            {
                return Conflict("A customer with this phone number already exists.");
            }

            // Get creator (logged-in user)
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? User.FindFirstValue("sub");

            // If no logged-in user (dev mode), keep dto value
            if (Guid.TryParse(performedBy, out var uid))
                dto.CreatedByUserId = uid.ToString();

            var result = await _service.CreateAsync(dto, performedBy!);

            return Ok(result);
        }

        // ============================================
        // UPDATE CUSTOMER
        // ============================================
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CustomerUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid update data.");

            var all = await _service.FilterAsync(null, null, null, null, null);

            // Duplicate email check
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var normalizedEmail = dto.Email.Trim().ToLower();

                if (all.Any(c =>
                    c.CustomerId != id &&
                    c.Email != null &&
                    c.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
                {
                    return Conflict("Another customer already uses this email.");
                }
            }

            // Duplicate phone check
            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                var normalizedPhone = dto.Phone.Trim();

                if (all.Any(c =>
                    c.CustomerId != id &&
                    c.Phone == normalizedPhone))
                {
                    return Conflict("Another customer already uses this phone number.");
                }
            }

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? User.FindFirstValue("sub");

            var updated = await _service.UpdateAsync(id, dto, performedBy!);

            return updated ? NoContent() : NotFound();
        }

        // ============================================
        // DELETE CUSTOMER
        // ============================================
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? User.FindFirstValue("sub");

            var deleted = await _service.DeleteAsync(id, performedBy!);
            return deleted ? NoContent() : NotFound();
        }

        // ============================================
        // PAGED
        // ============================================
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] PageParams parms)
        {
            var result = await _service.GetPagedAsync(parms);
            return Ok(result);
        }

        // ============================================
        // DASHBOARD ENDPOINTS
        // ============================================

        [HttpGet("count")]
        public async Task<IActionResult> GetTotalCount()
        {
            try
            {
                var count = await _service.GetTotalCountAsync();
                return Ok(count);
            }
            catch
            {
                return StatusCode(500, "Failed to get customer count");
            }
        }

        [HttpGet("new")]
        public async Task<IActionResult> GetNewCustomers([FromQuery] int days = 7)
        {
            try
            {
                var count = await _service.GetNewCustomersCountAsync(days);
                return Ok(count);
            }
            catch
            {
                return StatusCode(500, "Failed to get new customers count");
            }
        }
    }
}
