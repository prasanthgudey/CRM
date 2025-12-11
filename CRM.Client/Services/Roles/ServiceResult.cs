// *** NEW CODE START ***
// File: CRM.Client/Services/ServiceResult.cs
namespace CRM.Client.Services
{
    public class ServiceResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int? StatusCode { get; set; }
    }
}
// *** NEW CODE END ***
