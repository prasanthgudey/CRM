using System.ComponentModel.DataAnnotations;
using CRM.Server.Data;

namespace CRM.Server.DTOs
{
    public class CustomerResponseDto
    {
        public Guid CustomerId { get; set; }
        public string FirstName { get; set; }
        public string SurName { get; set; }
        public string? MiddleName { get; set; } //optional
        public string? PreferredName { get; set; } //optional
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }= string.Empty;
        public Guid CreatedByUserId { get; set; }
        public string CreatedAt { get; set; }
    }
}
