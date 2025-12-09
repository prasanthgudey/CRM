using Appointment_PP.Models;

namespace Appointment_PP.DTOs
{
    public class AppointmentResponseDto
    {
        public Guid AppointmentId { get; set; }

        public Guid CustomerId { get; set; }
        public Guid UserId { get; set; }

        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        public string Type { get; set; }
        public string Status { get; set; }

        public RecurrenceType RecurrenceType { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
