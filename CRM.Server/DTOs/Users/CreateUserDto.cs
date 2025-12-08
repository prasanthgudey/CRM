namespace CRM.Server.DTOs.Users
{
    public class CreateUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // ✅ Admin assigns role at creation
        public string Role { get; set; } = string.Empty;
    }
}
