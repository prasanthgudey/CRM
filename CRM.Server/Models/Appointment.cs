//namespace Appointment_PP.Models
//{
//    public class Appointment
//    {
//        public Guid AppointmentId { get; set; } = Guid.NewGuid();

//        public Guid CustomerId { get; set; }
//        public Customer Customer { get; set; }

//        public Guid UserId { get; set; }
//        public User User { get; set; }

//        public DateTime StartDateTime { get; set; }
//        public DateTime EndDateTime { get; set; }

//        public string Type { get; set; }      // Meeting / Call / Demo
//        public string Status { get; set; }    // Scheduled / Completed / Cancelled

//        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
//        public DateTime? RecurrenceEndDate { get; set; }

//        public string OutlookEventId { get; set; }   // for optional sync
//        public DateTime CreatedAt { get; set; }
//    }
//}
