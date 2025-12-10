namespace CRM.Client.DTOs.Users
{
    public class UserResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool TwoFactorEnabled { get; set; }

    }
}
