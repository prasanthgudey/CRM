using Microsoft.AspNetCore.Mvc;
using CRM.Server.Dtos;
using CRM.Server.Services;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _service;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService service, ILogger<TasksController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =============================
        // GET: /api/tasks/all
        // =============================

        /// <summary>
        /// Retrieves all tasks, optionally filtered and sorted.
        /// </summary>
        /// <returns>List of tasks.</returns>
        [HttpGet("all")]
        public IActionResult GetAll()
        {
            _logger.LogInformation("GET api/tasks/all called");

            var tasks = _service.GetAll();
            return Ok(tasks);
        }

        // =============================
        // GET: /api/tasks/{id}
        // =============================

        /// <summary>
        /// Gets a single task by its unique identifier.
        /// </summary>
        /// <param name="id">The Guid of the task.</param>
        /// <returns>Task details if found, otherwise 404.</returns>
        [HttpGet("{id:guid}")]
        public IActionResult GetById(Guid id)
        {
            _logger.LogInformation("GET api/tasks/{Id} called", id);

            var task = _service.GetById(id);
            if (task == null)
            {
                _logger.LogWarning("Task with Id {Id} not found", id);
                return NotFound("Task not found");
            }

            return Ok(task);
        }

        // =============================
        // GET: /api/tasks/customer/{customerId}
        // =============================

        /// <summary>
        /// Gets all tasks associated with a specific customer.
        /// </summary>
        /// <param name="customerId">The Guid of the customer.</param>
        /// <returns>List of tasks for that customer.</returns>
        [HttpGet("customer/{customerId:guid}")]
        public IActionResult GetByCustomerId(Guid customerId)
        {
            _logger.LogInformation("GET api/tasks/customer/{CustomerId} called", customerId);

            var result = _service.GetAll(new TaskFilterDto { CustomerId = customerId });
            return Ok(result);
        }

        // =============================
        // GET: /api/tasks/user/{userId}
        // =============================

        /// <summary>
        /// Gets all tasks created/owned by a specific user.
        /// </summary>
        /// <param name="userId">The user Id (from Identity).</param>
        /// <returns>List of tasks for that user.</returns>
        [HttpGet("user/{userId}")]
        public IActionResult GetByUserId(string userId)
        {
            _logger.LogInformation("GET api/tasks/user/{UserId} called", userId);

            var result = _service.GetAll(new TaskFilterDto { UserId = userId });
            return Ok(result);
        }

        // =============================
        // POST: /api/tasks
        // =============================

        /// <summary>
        /// Creates a new task.
        /// </summary>
        /// <param name="dto">Task data to create.</param>
        /// <returns>The created task with its Id.</returns>
        [HttpPost]
        public IActionResult Create([FromBody] CreateTaskDto dto)
        {
            _logger.LogInformation("POST api/tasks called");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Task create failed due to invalid model state");
                return BadRequest(ModelState);
            }

            var task = _service.Create(dto);

            _logger.LogInformation("Task created with Id {TaskId}", task.TaskId);

            return CreatedAtAction(nameof(GetById), new { id = task.TaskId }, task);
        }

        // =============================
        // PUT: /api/tasks/{id}
        // =============================

        /// <summary>
        /// Updates an existing task.
        /// </summary>
        /// <param name="id">The Guid of the task to update.</param>
        /// <param name="dto">Updated task data.</param>
        /// <returns>The updated task.</returns>
        [HttpPut("{id:guid}")]
        public IActionResult Update(Guid id, [FromBody] UpdateTaskDto dto)
        {
            _logger.LogInformation("PUT api/tasks/{Id} called", id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Task update failed for Id {Id} due to invalid model state", id);
                return BadRequest(ModelState);
            }

            // Any exception thrown here will be handled by GlobalExceptionMiddleware
            var task = _service.Update(id, dto);

            _logger.LogInformation("Task updated with Id {Id}", id);

            return Ok(task);
        }

        // =============================
        // DELETE: /api/tasks/{id}
        // =============================

        /// <summary>
        /// Deletes an existing task.
        /// </summary>
        /// <param name="id">The Guid of the task to delete.</param>
        /// <returns>NoContent if delete succeeds.</returns>
        [HttpDelete("{id:guid}")]
        public IActionResult Delete(Guid id)
        {
            _logger.LogInformation("DELETE api/tasks/{Id} called", id);

            // Any exception thrown here will be handled by GlobalExceptionMiddleware
            _service.Delete(id);

            _logger.LogInformation("Task deleted with Id {Id}", id);

            return NoContent();
        }
    }
}
