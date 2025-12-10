using CRM.Server.Models;
using CRM.Server.Models.Tasks;

namespace CRM.Server.Dtos
{
    public class TaskFilterDto
    {
        public Guid CustomerId { get; set; }

   
        public String UserId { get; set; }

    
        public TaskPriority? Priority { get; set; }
        public TaskState? State { get; set; }

        public DateTime? DueFrom { get; set; }
        public DateTime? DueTo { get; set; }
        

        // sortBy: "date", "priority", "status"
        public string? SortBy { get; set; }
        // "asc" or "desc"
        public string? SortDirection { get; set; } = "asc";
    }
}
