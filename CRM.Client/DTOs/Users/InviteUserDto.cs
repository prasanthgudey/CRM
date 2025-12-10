namespace CRM.Client.DTOs.Users
{
    public class InviteUserDto
    {
        public string FullName { get; set; } = string.Empty;  // ✅ REQUIRED
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

}
