using CRM.Server.Common.Paging;
using CRM.Server.Dtos;
using CRM.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // GET api/tasks?page=1&pageSize=20&search=foo
        [HttpGet("paged")]
        public async Task<IActionResult> GetAll([FromQuery] PageParams parms)
        {
            var result = await _service.GetPagedAsync(parms);
            return Ok(result);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("GET api/tasks/all called");
            try
            {
                var tasks = await _service.GetAllAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to GET api/tasks/all");
                return StatusCode(500, "Failed to get tasks");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("GET api/tasks/{Id} called", id);
            try
            {
                var task = await _service.GetByIdAsync(id);
                if (task == null)
                {
                    _logger.LogWarning("Task with Id {Id} not found", id);
                    return NotFound("Task not found");
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to GET api/tasks/{Id}", id);
                return StatusCode(500, "Failed to get task");
            }
        }

        [HttpGet("customer/{customerId:Guid}")]
        public async Task<IActionResult> GetByCustomerId(Guid customerId)
        {
            _logger.LogInformation("GET api/tasks/customer/{CustomerId} called", customerId);
            try
            {
                var result = await _service.GetAllAsync(new TaskFilterDto { CustomerId = customerId });
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to GET api/tasks/customer/{CustomerId}", customerId);
                return StatusCode(500, "Failed to get tasks for customer");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            _logger.LogInformation("GET api/tasks/user/{UserId} called", userId);
            try
            {
                var result = await _service.GetAllAsync(new TaskFilterDto { UserId = userId });
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to GET api/tasks/user/{UserId}", userId);
                return StatusCode(500, "Failed to get tasks for user");
            }
        }

        // -----------------------
        // Dashboard-friendly endpoints (added)
        // -----------------------

        /// <summary>
        /// GET: /api/tasks/count
        /// Returns the total number of tasks.
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetTotalCount()
        {
            _logger.LogInformation("GET api/tasks/count called");
            try
            {
                var count = await _service.GetTotalCountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get total tasks count");
                return StatusCode(500, "Failed to get tasks count");
            }
        }

        /// <summary>
        /// GET: /api/tasks/open/count
        /// Returns the number of open tasks (not completed).
        /// </summary>
        [HttpGet("open/count")]
        public async Task<IActionResult> GetOpenCount()
        {
            _logger.LogInformation("GET api/tasks/open/count called");
            try
            {
                var count = await _service.GetOpenCountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get open tasks count");
                return StatusCode(500, "Failed to get open tasks count");
            }
        }

        /// <summary>
        /// GET: /api/tasks/recent?take=50
        /// Returns recent tasks (paged by take).
        /// </summary>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent([FromQuery] int take = 50)
        {
            _logger.LogInformation("GET api/tasks/recent called (take={Take})", take);
            try
            {
                var tasks = await _service.GetRecentTasksAsync(take);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent tasks");
                return StatusCode(500, "Failed to get recent tasks");
            }
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

            try
            {
                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var task = await _service.CreateAsync(dto, performedBy!);
                _logger.LogInformation("Task created with Id {TaskId}", task.TaskId);
                return CreatedAtAction(nameof(GetById), new { id = task.TaskId }, task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create task");
                return StatusCode(500, "Failed to create task");
            }
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

                var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var task = await _service.UpdateAsync(id, dto, performedBy!);
                _logger.LogInformation("Task updated with Id {Id}", id);
                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update task with Id {Id}", id);
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
                _logger.LogError(ex, "Failed to delete task with Id {Id}", id);
                return BadRequest(ex.Message);
            }
        }
    }
}
