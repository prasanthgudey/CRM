using System;
using System.ComponentModel.DataAnnotations;

namespace CRM.Client.DTOs.Tasks
{
    public class CreateTaskDto
    {
        public Guid CustomerId { get; set; }
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public DateTime DueDate { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskState State { get; set; } = TaskState.Pending;

        // Recurring
        public bool IsRecurring { get; set; }
        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
        public int? RecurrenceInterval { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }
    }
}
