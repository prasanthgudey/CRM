using System;
using System.ComponentModel.DataAnnotations;
using CRM.Client.DTOs.Shared;


namespace CRM.Client.DTOs.Tasks
{
    public class CreateTaskDto
    {
        [Required(ErrorMessage = "Customer is required.")]
        public Guid? CustomerId { get; set; }

        [Required(ErrorMessage = "User is required.")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, ErrorMessage = "Title must be less than 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Due date is required.")]
        [DateNotInPast]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Priority is required.")]
        public TaskPriority Priority { get; set; }

        [Required]
        public TaskState State { get; set; } = TaskState.Pending;

        // Recurring -------------------------------

        public bool IsRecurring { get; set; }

        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;

        [Range(1, 365, ErrorMessage = "Recurrence interval must be between 1 and 365.")]
        public int? RecurrenceInterval { get; set; }

        [DataType(DataType.Date)]
        public DateTime? RecurrenceEndDate { get; set; }
    }
}