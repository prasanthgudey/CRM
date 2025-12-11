namespace CRM.Client.DTOs.Customers
{
    public class CustomerResponseDto
    {
        public Guid CustomerId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string SurName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }
        public string? PreferredName { get; set; }

        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public Guid CreatedByUserId { get; set; }
        public string CreatedAt { get; set; } = string.Empty;

        // Convenience property for UI
        public string FullName =>
            string.IsNullOrWhiteSpace(MiddleName)
                ? $"{FirstName} {SurName}"
                : $"{FirstName} {MiddleName} {SurName}";
    }
}
