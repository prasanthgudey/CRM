using Microsoft.AspNetCore.Mvc;
using CRM.Server.Dtos;
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
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService service, ILogger<TasksController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("GET api/tasks/all called");

            //var tasks = _service.GetAll();
            var tasks = await _service.GetAllAsync();
            return Ok(tasks);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("GET api/tasks/{Id} called", id);

            //var task = _service.GetById(id);
            var task = await _service.GetByIdAsync(id);
            if (task == null)
            {
                _logger.LogWarning("Task with Id {Id} not found", id);
                return NotFound("Task not found");
            }

            return Ok(task);
        }

        [HttpGet("customer/{customerId:Guid}")]
        public async Task<IActionResult> GetByCustomerId(Guid customerId)
        {
            _logger.LogInformation("GET api/tasks/customer/{CustomerId} called", customerId);

            //var result = _service.GetAll(new TaskFilterDto { CustomerId = customerId });
            var result = await _service.GetAllAsync(new TaskFilterDto { CustomerId = customerId });
            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            _logger.LogInformation("GET api/tasks/user/{UserId} called", userId);
            var result = await _service.GetAllAsync(new TaskFilterDto { UserId = userId });
            return Ok(result);
        }

        // =============================
        // ✅ CREATE TASK (AUDITED)
        // =============================

        /// <summary>
        /// Creates a new task.
        /// </summary>
        /// <param name="dto">Task data to create.</param>
        /// <returns>The created task with its Id.</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
        {
            _logger.LogInformation("POST api/tasks called");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Task create failed due to invalid model state");
                return BadRequest(ModelState);
            }

            //var task = _service.Create(dto);

            _logger.LogInformation("Task created with Id {TaskId}", task.TaskId);

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
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Task update failed for Id {Id} due to invalid model state", id);
                    return BadRequest(ModelState);
                }

                // Any exception thrown here will be handled by GlobalExceptionMiddleware
                //var task = _service.Update(id, dto);

                

                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var task = await _service.UpdateAsync(id, dto, performedBy!);
                _logger.LogInformation("Task updated with Id {Id}", id);
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
                _logger.LogInformation("DELETE api/tasks/{Id} called", id);
                await _service.DeleteAsync(id, performedBy!);
                _logger.LogInformation("Task deleted with Id {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
