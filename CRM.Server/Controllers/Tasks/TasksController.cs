using Microsoft.AspNetCore.Mvc;
using CRM.Server.Dtos;
using CRM.Server.Models.Tasks;
using CRM.Server.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // ✅ All task actions require login
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _service;

        public TasksController(ITaskService service)
        {
            _service = service;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var tasks = await _service.GetAllAsync();
            return Ok(tasks);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var task = await _service.GetByIdAsync(id);
            if (task == null)
                return NotFound("Task not found");

            return Ok(task);
        }

        [HttpGet("customer/{customerId:Guid}")]
        public async Task<IActionResult> GetByCustomerId(Guid customerId)
        {
            var result = await _service.GetAllAsync(new TaskFilterDto { CustomerId = customerId });
            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            var result = await _service.GetAllAsync(new TaskFilterDto { UserId = userId });
            return Ok(result);
        }

        // =============================
        // ✅ CREATE TASK (AUDITED)
        // =============================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _service.CreateAsync(dto, performedBy!);
            return CreatedAtAction(nameof(GetById), new { id = task.TaskId }, task);
        }

        // =============================
        // ✅ UPDATE TASK (AUDITED)
        // =============================
        [HttpPut("{id:Guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskDto dto)
        {
            try
            {
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var task = await _service.UpdateAsync(id, dto, performedBy!);
                return Ok(task);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =============================
        // ✅ DELETE TASK (AUDITED)
        // =============================
        [HttpDelete("{id:Guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _service.DeleteAsync(id, performedBy!);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
