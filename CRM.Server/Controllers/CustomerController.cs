using CRM.Server.Common.Paging;
using CRM.Server.DTOs;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.Server.Controllers
{
    [ApiController]
    //[Authorize]
    [Route("api/customers")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _service;

        public CustomerController(ICustomerService service)
        {
            _service = service;
        }

        // GET: api/customers (with filtering)
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

        [HttpPost]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(CustomerCreateDto dto)
        {
            // ⭐ ADDED: Load all customers using FilterAsync (NO GetAllAsync)
            var all = await _service.FilterAsync(null, null, null, null, null);

            var normalizedEmail = dto.Email?.Trim().ToLower();
            var normalizedPhone = dto.Phone?.Trim();

            // ⭐ ADDED: Duplicate email check
            if (!string.IsNullOrWhiteSpace(normalizedEmail) &&
                all.Any(c => c.Email != null &&
                             c.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict("A customer with this email already exists.");
            }

            // ⭐ ADDED: Duplicate phone check
            if (!string.IsNullOrWhiteSpace(normalizedPhone) &&
                all.Any(c => c.Phone == normalizedPhone))
            {
                return Conflict("A customer with this phone number already exists.");
            }

            // Set performedByUserId from authenticated user if available
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // If dto has CreatedByUserId property, prefer server side set
            dto.CreatedByUserId = performedBy;

            var result = await _service.CreateAsync(dto, performedBy!);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Update(Guid id, CustomerUpdateDto dto)
        {
            // ⭐ ADDED: Load all customers (no service changes, no GetAllAsync)
            var all = await _service.FilterAsync(null, null, null, null, null);

            // ⭐ ADDED: Duplicate email check
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

            // ⭐ ADDED: Duplicate phone check
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

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var updated = await _service.UpdateAsync(id, dto, performedBy!);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id:guid}")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var deleted = await _service.DeleteAsync(id, performedBy!);
            return deleted ? NoContent() : NotFound();
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] PageParams parms)
        {
            var result = await _service.GetPagedAsync(parms);
            return Ok(result);
        }

        // -------------------------
        // NEW: Dashboard endpoints
        // -------------------------

        [HttpGet("count")]
        public async Task<IActionResult> GetTotalCount()
        {
            try
            {
                var count = await _service.GetTotalCountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                // You can add logger if desired; keeping same style as other methods
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
            catch (Exception ex)
            {
                return StatusCode(500, "Failed to get new customers count");
            }
        }
    }
}
