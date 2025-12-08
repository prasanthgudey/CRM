using System.ComponentModel.DataAnnotations;

namespace CRM.Server.DTOs
{
    public class CreateCustomerDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name can't exceed 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone can't exceed 20 characters")]
        public string Phone { get; set; }

        [StringLength(200, ErrorMessage = "Address can't exceed 200 characters")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Creator Id is required")]
        public string CreatedByUserId { get; set; }

    }
}
