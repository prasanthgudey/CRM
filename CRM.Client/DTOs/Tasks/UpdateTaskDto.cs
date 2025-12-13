using System;
using System.ComponentModel.DataAnnotations;

namespace CRM.Client.DTOs.Tasks
{
    public class UpdateTaskDto
    {
        [StringLength(100, ErrorMessage = "Title must be less than 100 characters.")]
        public string? Title { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        public TaskPriority? Priority { get; set; }

        public TaskState? State { get; set; }

        // Recurring ------------------------------

        public bool IsRecurring { get; set; }

        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;

        [Range(1, 365, ErrorMessage = "Recurrence interval must be between 1 and 365.")]
        public int? RecurrenceInterval { get; set; }

        [DataType(DataType.Date)]
        public DateTime? RecurrenceEndDate { get; set; }
    }
}