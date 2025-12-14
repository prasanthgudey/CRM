using CRM.Server.Models.Tasks;

namespace CRM.Server.Dtos
{
    public class UpdateTaskDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskPriority? Priority { get; set; }
        public TaskState? State { get; set; }

        // ✅ ADD THESE
        public Guid? CustomerId { get; set; }
        public string? UserId { get; set; }

        // Recurring
        public bool? IsRecurring { get; set; }
        public RecurrenceType? RecurrenceType { get; set; }
        public int? RecurrenceInterval { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }
    }
}
