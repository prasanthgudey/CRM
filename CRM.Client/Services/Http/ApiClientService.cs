using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CRM.Client.Config;
using CRM.Client.State;
using CRM.Client.Services.Auth;
using CRM.Client.Security;
using Microsoft.Extensions.Options;

namespace CRM.Client.Services.Http
{
    // ✅ USER-DEFINED: Central HTTP communication service
    public class ApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;
        private readonly AppState _appState;
        private readonly TokenService _tokenService;
        private readonly JwtAuthStateProvider _jwtAuthStateProvider;

        public ApiClientService(
            HttpClient httpClient,
            IOptions<ApiSettings> apiOptions,
            AppState appState,
            TokenService tokenService,
            JwtAuthStateProvider jwtAuthStateProvider)
        {
            _httpClient = httpClient;
            _apiSettings = apiOptions.Value;
            _appState = appState;
            _tokenService = tokenService;
            _jwtAuthStateProvider = jwtAuthStateProvider;

            _httpClient.BaseAddress = new Uri(_apiSettings.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_apiSettings.TimeoutSeconds);

            Console.WriteLine($"[HTTP INIT] BaseUrl = {_httpClient.BaseAddress}");
        }

        // ✅ RESTORE + ATTACH JWT
        private async Task AttachTokenAsync()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (_appState.Auth == null || !_appState.Auth.IsAuthenticated)
            {
                var token = await _tokenService.GetTokenAsync();

                if (!string.IsNullOrWhiteSpace(token))
                {
                    var user = _jwtAuthStateProvider.BuildClaimsPrincipal(token);
                    _appState.Auth.SetAuthenticated(token, user);

                    Console.WriteLine("[HTTP] Auth restored from localStorage");
                }
            }

            if (_appState.Auth != null &&
                _appState.Auth.IsAuthenticated &&
                !string.IsNullOrWhiteSpace(_appState.Auth.Token))
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

        // ✅ =========================
        // ✅ GENERIC GET (RETURNS DATA)
        // ✅ =========================
        public async Task<T?> GetAsync<T>(string endpoint)
        {
            await AttachTokenAsync();

            Console.WriteLine($"[GET] {endpoint}");

            var response = await _httpClient.GetAsync(endpoint);

            Console.WriteLine($"[GET STATUS] {(int)response.StatusCode} {response.StatusCode}");

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[GET RESPONSE] {content}");

            return JsonSerializer.Deserialize<T>(content, JsonOptions());
        }

        // ✅ =====================================
        // ✅ GENERIC POST (RETURNS DATA — LOGIN)
        // ✅ =====================================
        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            await AttachTokenAsync();

            var json = JsonSerializer.Serialize(data, JsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"[POST] {endpoint}");
            Console.WriteLine($"[POST BODY] {json}");

            var response = await _httpClient.PostAsync(endpoint, content);

            Console.WriteLine($"[POST STATUS] {(int)response.StatusCode} {response.StatusCode}");

            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[POST RESPONSE] {responseJson}");

            if (!response.IsSuccessStatusCode)
                return default;

            // ✅ CRITICAL SAFETY CHECK (prevents ghost failures)
            if (string.IsNullOrWhiteSpace(responseJson))
                return default;

            return JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions());
        }

        // ✅ ======================================
        // ✅ GENERIC POST (NO DATA — ACTION ONLY)
        // ✅ ======================================
        public async Task PostAsync<TRequest>(string endpoint, TRequest data)
        {
            await AttachTokenAsync();

            var json = JsonSerializer.Serialize(data, JsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"[POST] {endpoint}");
            Console.WriteLine($"[POST BODY] {json}");

            var response = await _httpClient.PostAsync(endpoint, content);

            Console.WriteLine($"[POST STATUS] {(int)response.StatusCode} {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }
        }

        // ✅ =========================
        // ✅ GENERIC PUT (SAFE)
        // ✅ =========================
        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            await AttachTokenAsync();

            var json = JsonSerializer.Serialize(data, JsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"[PUT] {endpoint}");
            Console.WriteLine($"[PUT BODY] {json}");

            var response = await _httpClient.PutAsync(endpoint, content);

            Console.WriteLine($"[PUT STATUS] {(int)response.StatusCode} {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
                return default;

            var responseJson = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[PUT RESPONSE] {responseJson}");

            // ✅ SAFETY CHECK
            if (string.IsNullOrWhiteSpace(responseJson))
                return default;

            return JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions());
        }

        // ✅ =========================
        // ✅ GENERIC DELETE (NO DATA)
        // ✅ =========================
        public async Task DeleteAsync(string endpoint)
        {
            await AttachTokenAsync();

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
