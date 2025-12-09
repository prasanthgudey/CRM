using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CRM.Client.Config;
using CRM.Client.State;

namespace CRM.Client.Services.Http
{
    // ✅ USER-DEFINED: Central HTTP communication service
    public class ApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;
        private readonly AppState _appState;

        public ApiClientService(
            HttpClient httpClient,
            ApiSettings apiSettings,
            AppState appState)
        {
            _httpClient = httpClient;
            _apiSettings = apiSettings;
            _appState = appState;

            _httpClient.BaseAddress = new Uri(_apiSettings.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_apiSettings.TimeoutSeconds);

            Console.WriteLine($"[HTTP INIT] BaseUrl = {_httpClient.BaseAddress}");
        }

        // ✅ USER-DEFINED: Prepares Authorization header
        private void AttachToken()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (_appState.Auth.IsAuthenticated && !string.IsNullOrWhiteSpace(_appState.Auth.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _appState.Auth.Token);

                Console.WriteLine("[HTTP] Bearer token attached");
            }
            else
            {
                Console.WriteLine("[HTTP] No token attached");
            }
        }

        // ✅ GENERIC GET
        public async Task<T?> GetAsync<T>(string endpoint)
        {
            AttachToken();

            Console.WriteLine($"[GET] {endpoint}");

            var response = await _httpClient.GetAsync(endpoint);

            Console.WriteLine($"[GET STATUS] {(int)response.StatusCode} {response.StatusCode}");

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[GET RESPONSE] {content}");

            return JsonSerializer.Deserialize<T>(content, JsonOptions());
        }

        // ✅ ✅ ✅ FIXED GENERIC POST (LOGIN SAFE)
        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            AttachToken();

            var json = JsonSerializer.Serialize(data, JsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"[POST] {endpoint}");
            Console.WriteLine($"[POST BODY] {json}");

            var response = await _httpClient.PostAsync(endpoint, content);

            Console.WriteLine($"[POST STATUS] {(int)response.StatusCode} {response.StatusCode}");

            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[POST RESPONSE] {responseJson}");

            // ✅ IMPORTANT FIX: Do NOT crash on 401/403
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("[POST] Request failed, returning null safely.");
                return default;
            }

            return JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions());
        }

        // ✅ GENERIC PUT
        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            AttachToken();

            var json = JsonSerializer.Serialize(data, JsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"[PUT] {endpoint}");
            Console.WriteLine($"[PUT BODY] {json}");

            var response = await _httpClient.PutAsync(endpoint, content);

            Console.WriteLine($"[PUT STATUS] {(int)response.StatusCode} {response.StatusCode}");

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[PUT RESPONSE] {responseJson}");

            return JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions());
        }

        // ✅ GENERIC DELETE
        public async Task DeleteAsync(string endpoint)
        {
            AttachToken();

            Console.WriteLine($"[DELETE] {endpoint}");

            var response = await _httpClient.DeleteAsync(endpoint);

            Console.WriteLine($"[DELETE STATUS] {(int)response.StatusCode} {response.StatusCode}");

            response.EnsureSuccessStatusCode();
        }

        // ✅ USER-DEFINED: Shared JSON options
        private static JsonSerializerOptions JsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }
    }
}
