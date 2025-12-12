using System.ComponentModel.DataAnnotations;

namespace CRM.Server.Models.Tasks

{
    public class TaskItem
    {
        [Key]
        public Guid TaskId { get; set; }

        // Link to Customer
        public Guid CustomerId { get; set; }
        // 🔹 NEW: Link to User who created/owns this task

        public Customer Customer { get; set; }
        public string CreatedByUserId { get; set; }

        public ApplicationUser? CreatedBy { get; set; }
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        public TaskPriority Priority { get; set; }

        [Required]
        public TaskState State { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Recurring
        public bool IsRecurring { get; set; }
        public RecurrenceType RecurrenceType { get; set; }
        public int? RecurrenceInterval { get; set; }       // e.g. every 2 days/weeks/months
        public DateTime? RecurrenceEndDate { get; set; }   // null = no end
    }
}
