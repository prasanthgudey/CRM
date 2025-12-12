using CRM.Client.DTOs.Shared;
using System.Net.Http.Json;
using System.Text.Json;

namespace CRM.Client.Services
{
    public class SearchService
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public SearchService(HttpClient http)
        {
            _http = http;
            Console.WriteLine($"[SearchService] BaseAddress = {_http.BaseAddress}");
        }

        public async Task<List<SearchResultDto>> SearchAsync(string q, int limit = 25)
        {
            if (string.IsNullOrWhiteSpace(q)) return new List<SearchResultDto>();

            // Use absolute API route starting at host root
            var url = $"/api/search?q={Uri.EscapeDataString(q)}&limit={limit}";

            try
            {
                // Optional: you can log the full request URL to the browser console for debugging
                Console.WriteLine($"[SearchService] GET {(_http.BaseAddress?.ToString().TrimEnd('/') ?? "")}{url}");

                var results = await _http.GetFromJsonAsync<List<SearchResultDto>>(url, _jsonOptions);
                return results ?? new List<SearchResultDto>();
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"[SearchService] HTTP error: {ex.Message}");
                return new List<SearchResultDto>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SearchService] error: {ex.Message}");
                return new List<SearchResultDto>();
            }
        }
    }
}
