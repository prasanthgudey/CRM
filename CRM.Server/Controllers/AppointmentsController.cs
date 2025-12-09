using Appointment_PP.DTOs;
using Appointment_PP.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Appointment_PP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _service;

        public AppointmentsController(IAppointmentService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<ActionResult<AppointmentResponseDto>> Create([FromBody] AppointmentCreateDto dto)
        {
            var created = await _service.CreateAppointmentAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.AppointmentId }, created);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AppointmentResponseDto>> GetById(Guid id)
        {
            var appt = await _service.GetAppointmentByIdAsync(id);
            if (appt == null) return NotFound();
            return Ok(appt);
        }

        [HttpGet("by-customer/{customerId:guid}")]
        public async Task<ActionResult<IEnumerable<AppointmentResponseDto>>> GetByCustomer(Guid customerId)
        {
            var list = await _service.GetAppointmentsByCustomerAsync(customerId);
            return Ok(list);
        }

        // Day view: /api/appointments/day?date=2025-12-08
        [HttpGet("day")]
        public async Task<ActionResult<IEnumerable<AppointmentInstanceDto>>> GetDay([FromQuery] DateTime date)
        {
            var from = date.Date;
            var to = date.Date.AddDays(1).AddTicks(-1);
            var list = await _service.GetAppointmentsInRangeAsync(from, to);
            return Ok(list);
        }

        // Week view: /api/appointments/week?start=2025-12-08
        [HttpGet("week")]
        public async Task<ActionResult<IEnumerable<AppointmentInstanceDto>>> GetWeek([FromQuery] DateTime start)
        {
            var from = start.Date;
            var to = start.Date.AddDays(7).AddTicks(-1);
            var list = await _service.GetAppointmentsInRangeAsync(from, to);
            return Ok(list);
        }

        // Month view: /api/appointments/month?year=2025&month=12
        [HttpGet("month")]
        public async Task<ActionResult<IEnumerable<AppointmentInstanceDto>>> GetMonth(
            [FromQuery] int year,
            [FromQuery] int month)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddTicks(-1);
            var list = await _service.GetAppointmentsInRangeAsync(from, to);
            return Ok(list);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<AppointmentResponseDto>> Update(Guid id, [FromBody] AppointmentUpdateDto dto)
        {
            var updated = await _service.UpdateAppointmentAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAppointmentAsync(id);
            if (!success) return NotFound();
            // FE should show confirmation before calling this endpoint
            return NoContent();
        }
    }
}
