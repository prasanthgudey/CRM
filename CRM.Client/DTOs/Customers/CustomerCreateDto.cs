using System.ComponentModel.DataAnnotations;

namespace CRM.Client.DTOs.Customers
{
    public class CustomerCreateDto
    {
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Surname is required.")]
        public string SurName { get; set; } = string.Empty;

        public string? PreferredName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(10, ErrorMessage = "Phone must be 10 digits.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Created By User is required.")]
        public Guid CreatedByUserId { get; set; }
    }

}
