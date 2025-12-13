using System.ComponentModel.DataAnnotations;

namespace CRM.Client.DTOs.Customers
{
    public class CustomerUpdateDto
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name can't exceed 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        [StringLength(100, ErrorMessage = "Surname can't exceed 100 characters")]
        public string SurName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }
        public string? PreferredName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(10, ErrorMessage = "Phone can't exceed 10 characters")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(200, ErrorMessage = "Address can't exceed 200 characters")]
        public string Address { get; set; } = string.Empty;
    }
}
