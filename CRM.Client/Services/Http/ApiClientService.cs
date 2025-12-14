using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

using CRM.Client.Config;
using CRM.Client.DTOs.Auth;
using CRM.Client.Security;
using CRM.Client.Services.Auth;
using CRM.Client.State;

namespace CRM.Client.Services.Http
{
    //  USER-DEFINED: Central HTTP communication service (REFRESH-AWARE + session handling)
    public class ApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;
        private readonly AppState _appState;
        private readonly TokenService _tokenService;
        private readonly JwtAuthStateProvider _jwtAuthStateProvider;
        private readonly NavigationManager _navigation;

        // single-flight refresh guard
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

        public ApiClientService(
      HttpClient httpClient,
      ApiSettings apiSettings,           
      AppState appState,
      TokenService tokenService,
      JwtAuthStateProvider jwtAuthStateProvider,
      NavigationManager navigationManager)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiSettings = apiSettings ?? throw new ArgumentNullException(nameof(apiSettings));
            _appState = appState ?? throw new ArgumentNullException(nameof(appState));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _jwtAuthStateProvider = jwtAuthStateProvider ?? throw new ArgumentNullException(nameof(jwtAuthStateProvider));
            _navigation = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));

            if (string.IsNullOrWhiteSpace(_apiSettings.BaseUrl))
                throw new InvalidOperationException("ApiSettings.BaseUrl is not configured. Add it to wwwroot/appsettings.json");

            var baseUrl = _apiSettings.BaseUrl.EndsWith("/") ? _apiSettings.BaseUrl : _apiSettings.BaseUrl + "/";
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_apiSettings.TimeoutSeconds);
        }



        // -------------------------
        // Attach token (restore if needed)
        // -------------------------
        private async Task AttachTokenAsync()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if ((_appState.Auth == null || !_appState.Auth.IsAuthenticated))
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

        // -------------------------
        // Try refresh token (single-flight)
        // -------------------------
        private async Task<bool> TryRefreshTokenAsync()
        {
            // If no refresh token saved -> cannot refresh
            var refreshToken = await _tokenService.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                Console.WriteLine("[REFRESH] No refresh token available");
                return false;
            }

            // Quick check: if refresh expiry set and already passed, bail early
            var expiresAt = await _tokenService.GetRefreshExpiresAtAsync();
            if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
            {
                Console.WriteLine("[REFRESH] Refresh token already expired (client-side)");
                return false;
            }

            // Capture the access token that triggered this refresh attempt.
            // If another flow refreshes while we wait, we'll detect it and skip calling the backend.
            var originalAccessToken = _appState.Auth?.Token ?? await _tokenService.GetTokenAsync();

            await _refreshLock.WaitAsync();
            try
            {
                // Another caller might already have refreshed while we waited — re-check the current token.
                var currentToken = _appState.Auth?.Token ?? await _tokenService.GetTokenAsync();

                // If token changed (and is non-empty) then someone else already refreshed successfully.
                if (!string.IsNullOrWhiteSpace(currentToken) && currentToken != originalAccessToken)
                {
                    Console.WriteLine("[REFRESH] Token was already refreshed by another flow. Skipping refresh call.");
                    return true;
                }

                Console.WriteLine("[REFRESH] Calling /api/auth/refresh");

                var payload = JsonSerializer.Serialize(new { RefreshToken = refreshToken }, JsonOptions());
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                HttpResponseMessage resp;
                try
                {
                    resp = await _httpClient.PostAsync("api/auth/refresh", content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[REFRESH] HTTP call failed: " + ex.Message);
                    return false;
                }

                var respJson = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"[REFRESH STATUS] {(int)resp.StatusCode} {resp.StatusCode}");
                Console.WriteLine($"[REFRESH RESPONSE] {respJson}");

                // If server returned 401/invalid_token or session_expired -> force logout
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Try to parse error body
                    try
                    {
                        var err = JsonSerializer.Deserialize<Dictionary<string, string>>(respJson, JsonOptions());
                        if (err != null && err.TryGetValue("error", out var code) && code == "session_expired")
                        {
                            Console.WriteLine("[REFRESH] Session expired reported by server -> forcing logout");
                            await ForceLogoutAsync("Your session has expired. Please login again.");
                            return false;
                        }
                    }
                    catch { /* ignore parse errors */ }

                    Console.WriteLine("[REFRESH] Refresh returned 401 -> invalid token");
                    await ForceLogoutAsync("Authentication failed. Please login again.");
                    return false;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine("[REFRESH] Refresh failed with non-success status");
                    return false;
                }

                // Parse AuthResponseDto
                AuthResponseDto? authResp = null;
                try
                {
                    authResp = JsonSerializer.Deserialize<AuthResponseDto>(respJson, JsonOptions());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[REFRESH] Failed to parse response: " + ex.Message);
                    return false;
                }

                if (authResp == null || string.IsNullOrWhiteSpace(authResp.Token))
                {
                    Console.WriteLine("[REFRESH] No token returned during refresh");
                    return false;
                }

                // 1) Save access token (persistent)
                await _tokenService.SaveTokenAsync(authResp.Token);

                // 2) Update in-memory app state and http client header
                var principal = _jwtAuthStateProvider.BuildClaimsPrincipal(authResp.Token);
                _appState.Auth.SetAuthenticated(authResp.Token, principal);

                try
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", authResp.Token);
                }
                catch { /* swallow if header update fails, but we still proceed */ }

                // 3) Notify Blazor auth provider (updates UI)
                await _jwtAuthStateProvider.MarkUserAsAuthenticated(authResp.Token);

                // 4) Save refresh token (may be rotated) and expiry
                if (!string.IsNullOrWhiteSpace(authResp.RefreshToken))
                {
                    await _tokenService.SaveRefreshTokenAsync(authResp.RefreshToken, authResp.RefreshExpiresAt);
                }

                Console.WriteLine("[REFRESH] Refresh successful, tokens updated");
                return true;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        // -------------------------
        // Force Logout helper: clear tokens, set UI message and navigate to login
        // -------------------------
        private async Task ForceLogoutAsync(string? message = null)
        {
            try
            {
                // clear local tokens & app state
                await _tokenService.ClearAsync();
                await _jwtAuthStateProvider.MarkUserAsLoggedOut();
                _appState.Auth.Clear();

                if (!string.IsNullOrWhiteSpace(message))
                {
                    _appState.Ui.ShowGlobalMessage(message, "warning");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AUTH] ForceLogout failed: " + ex.Message);
            }

            // navigate to login page (use absolute navigation to force reload)
            _navigation.NavigateTo("/login", true);
        }

        // -------------------------
        // Generic retry wrapper: tries request, on 401 attempts refresh+retry once
        // -------------------------
        private async Task<HttpResponseMessage> SendWithRefreshAsync(Func<Task<HttpResponseMessage>> requestFactory)
        {
            // 1) first attempt
            var response = await requestFactory();

            // 2) if success -> return
            if (response.IsSuccessStatusCode)
                return response;

            // ------------------------------------------------------
            // 3) Password expired handling (403 + error: password_expired)
            // ------------------------------------------------------
            // Password expired -> server returns 403 + { error: "password_expired" }
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                var body = await response.Content.ReadAsStringAsync();
                try
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(body, JsonOptions());
                    if (dict != null && dict.TryGetValue("error", out var code) && code == "password_expired")
                    {
                        _appState.Ui.ShowGlobalMessage("Your password has expired. Please change your password to continue.", "warning", 8);

                        // navigate to change-expired-password (use absolute URI or path)
                        _navigation.NavigateTo("/change-expired-password", true);

                        return response;
                    }
                }
                catch { /* ignore parse errors */ }
            }


            // 4) check for session_expired payload (401 + { error: "session_expired" })
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var body = await response.Content.ReadAsStringAsync();
                
                try
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(body, JsonOptions());
                    if (dict != null && dict.TryGetValue("error", out var err) && err == "session_expired")
                    {
                        _appState.Ui.ShowGlobalMessage("Your session has expired. Please log in again.", "warning", 8);
                        await ForceLogoutAsync("Your session has expired. Please log in again.");
                        return response;
                    }
                }
                catch { /* ignore parse errors */ }


                // Attempt to refresh
                var refreshed = await TryRefreshTokenAsync();
                if (!refreshed)
                {
                    // refresh failed -> return original 401
                    return response;
                }

                // Retry once after successful refresh
                var retryResp = await requestFactory();
                return retryResp;
            }

            return response;
        }

        // =========================
        // Generic GET (returns data)
        // =========================
        public async Task<T?> GetAsync<T>(string endpoint)
        {
            await AttachTokenAsync();

            var response = await SendWithRefreshAsync(() => _httpClient.GetAsync(endpoint));

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<T>(content, JsonOptions());
        }


        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            await AttachTokenAsync();

            var json = JsonSerializer.Serialize(data, JsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await SendWithRefreshAsync(() => _httpClient.PostAsync(endpoint, content));

            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return default;

            if (string.IsNullOrWhiteSpace(responseJson))
                return default;

            return JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions());
        }


        public async Task<HttpResponseMessage> PostRawAsync<TRequest>(string endpoint, TRequest data)
        {
            await AttachTokenAsync();

            var json = JsonSerializer.Serialize(data, JsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await SendWithRefreshAsync(() => _httpClient.PostAsync(endpoint, content));

            return response;
        }


        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            await AttachTokenAsync();

            var json = JsonSerializer.Serialize(data, JsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await SendWithRefreshAsync(() => _httpClient.PutAsync(endpoint, content));

            if (!response.IsSuccessStatusCode)
                return default;

            var responseJson = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseJson))
                return default;

            return JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions());
        }


        public async Task PutAsync<TRequest>(string endpoint, TRequest data)
        {
            await AttachTokenAsync();

            var json = JsonSerializer.Serialize(data, JsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await SendWithRefreshAsync(() => _httpClient.PutAsync(endpoint, content));

            response.EnsureSuccessStatusCode();
        }

        public async Task<HttpResponseMessage> PutRawAsync<TRequest>(string endpoint, TRequest data)
        {
            await AttachTokenAsync();

            var json = JsonSerializer.Serialize(data, JsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await SendWithRefreshAsync(() => _httpClient.PutAsync(endpoint, content));

            return response;
        }

        public async Task DeleteAsync(string endpoint)
        {
            await AttachTokenAsync();

            var response = await SendWithRefreshAsync(() => _httpClient.DeleteAsync(endpoint));

            response.EnsureSuccessStatusCode();
        }

        // Shared JSON options
        private static JsonSerializerOptions JsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }
        public async Task<HttpResponseMessage> PostRawAsync(string url, object data)
        {
            return await _httpClient.PostAsJsonAsync(url, data);
        }

    }
}
