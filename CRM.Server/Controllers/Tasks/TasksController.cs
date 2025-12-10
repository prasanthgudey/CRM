using Microsoft.AspNetCore.Mvc;
using CRM.Server.Dtos;
using CRM.Server.Models.Tasks;
using CRM.Server.Services;

namespace CRM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _service;

        public TasksController(ITaskService service)
        {
            _service = service;
        }

       


        [HttpGet("all")]
        public IActionResult GetAll()
        {
            var tasks =  _service.GetAll();
            return Ok(tasks);
        }

        // =============================
        // GET: /api/tasks/{id}
        // =============================
        [HttpGet("{id:guid}")]
        public IActionResult GetById(Guid id)
        {
            var task = _service.GetById(id);
            if (task == null)
                return NotFound("Task not found");

            return Ok(task);
        }


        // =============================
        // GET: /api/tasks/customer/{customerId}
        // =============================
        [HttpGet("customer/{customerId:Guid}")]
        public IActionResult GetByCustomerId(Guid customerId)
        {
            var result = _service.GetAll(new TaskFilterDto { CustomerId = customerId });
            return Ok(result);
        }
        // =============================
        // GET: /api/tasks/user/{userId}
        // =============================
        [HttpGet("user/{userId}")]
        public IActionResult GetByUserId(string userId)
        {
            var result = _service.GetAll(new TaskFilterDto { UserId = userId });
            return Ok(result);
        }


        // =============================
        // POST: /api/tasks
        // =============================
        [HttpPost]
        public IActionResult Create([FromBody] CreateTaskDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var task = _service.Create(dto);
            return CreatedAtAction(nameof(GetById), new { id = task.TaskId }, task);
        }

        // =============================
        // PUT: /api/tasks/{id}
        // =============================
        [HttpPut("{id:Guid}")]
        public IActionResult Update(Guid id, [FromBody] UpdateTaskDto dto)
        {
            try
            {
                var task = _service.Update(id, dto);
                return Ok(task);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =============================
        // DELETE: /api/tasks/{id}
        // =============================
        [HttpDelete("{id:Guid}")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                _service.Delete(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
