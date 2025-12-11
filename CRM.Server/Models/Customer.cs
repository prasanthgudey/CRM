using CRM.Server.Data;

namespace CRM.Server.Models
{
    public class Customer
    {
        public Guid CustomerId {  get; set; }
        //add first name and surname properties instead of full name
        //public string FullName { get; set; }

        public string FirstName { get; set; }
        public string SurName { get; set; }

        public string? MiddleName { get; set; } //optional
        public string? PreferredName { get; set; } //optional

        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string CreatedByUserId { get; set; }
        public string CreatedAt { get; set; }
    }
}
