using CRM.Server.Common.Paging;
using CRM.Server.DTOs;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
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

    }
}
