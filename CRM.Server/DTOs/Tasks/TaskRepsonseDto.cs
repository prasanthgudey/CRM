using CRM.Server.Models;
using CRM.Server.Models.Tasks;

namespace CRM.Server.Dtos
{
    public class TaskResponseDto
    {
        public Guid TaskId { get; set; }
      

        public Guid CustomerId { get; set; }
        public String UserId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public DateTime DueDate { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskState State { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public bool IsRecurring { get; set; }
        public RecurrenceType RecurrenceType { get; set; }
        public int? RecurrenceInterval { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }
    }
}
