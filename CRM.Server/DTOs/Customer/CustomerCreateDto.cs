using System.ComponentModel.DataAnnotations;
using CRM.Server.Data;


namespace CRM.Server.DTOs
{
    public class CustomerCreateDto
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "Full name can't exceed 100 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Surname is required")]
        [StringLength(100, ErrorMessage = "Surname can't exceed 100 characters")]
        public string SurName { get; set; }

        public string? MiddleName { get; set; } //optional
        public string? PreferredName { get; set; } //optional

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone can't exceed 20 characters")]
        public string Phone { get; set; }

        [StringLength(200, ErrorMessage = "Address can't exceed 200 characters")]
        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }= string.Empty;

        [Required(ErrorMessage = "Creator Id is required")]
        public string CreatedByUserId { get; set; }

    }
}
