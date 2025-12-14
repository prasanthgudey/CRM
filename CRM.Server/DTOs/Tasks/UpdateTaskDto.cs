using System;
using System.ComponentModel.DataAnnotations;
using CRM.Server.Models.Tasks;

namespace CRM.Server.Dtos
{
    public class UpdateTaskDto
    {
        // -------------------------
        // Task details (optional)
        // -------------------------

        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string? Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        public TaskPriority? Priority { get; set; }

        public TaskState? State { get; set; }

        // -------------------------
        // Recurring settings (optional)
        // -------------------------

        public bool? IsRecurring { get; set; }

        public RecurrenceType? RecurrenceType { get; set; }

        [Range(1, 365, ErrorMessage = "Recurrence interval must be between 1 and 365")]
        public int? RecurrenceInterval { get; set; }

        public DateTime? RecurrenceEndDate { get; set; }
    }
}
