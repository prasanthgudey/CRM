// ⭐ new DTO file
namespace CRM.Client.DTOs.Users
{
    public class OperationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
