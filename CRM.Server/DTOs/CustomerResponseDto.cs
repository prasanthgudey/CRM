namespace CRM.Server.DTOs
{
    public class CustomerResponseDto
    {
        public Guid CustomerId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string CreatedByUserId { get; set; }
        public string CreatedAt { get; set; }
    }
}
