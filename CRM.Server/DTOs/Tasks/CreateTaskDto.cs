using System.ComponentModel.DataAnnotations;
using CRM.Server.Models.Tasks;

namespace CRM.Server.Dtos
{
    public class CreateTaskDto
    {
        // -------------------------
        // Ownership
        // -------------------------

        [Required(ErrorMessage = "CustomerId is required")]
        public Guid CustomerId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public string UserId { get; set; } = string.Empty;

        // -------------------------
        // Task details
        // -------------------------

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        public TaskPriority Priority { get; set; }

        public TaskState State { get; set; } = TaskState.Pending;

        // -------------------------
        // Recurring settings
        // -------------------------

        public bool IsRecurring { get; set; }

        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;

        [Range(1, 365, ErrorMessage = "Recurrence interval must be between 1 and 365")]
        public int? RecurrenceInterval { get; set; }

        public DateTime? RecurrenceEndDate { get; set; }
    }
}
