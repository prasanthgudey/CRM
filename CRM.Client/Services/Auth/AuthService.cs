using CRM.Client.DTOs.Auth;
using CRM.Client.DTOs.Users;
using CRM.Client.Security;
using CRM.Client.Services.Http;
using CRM.Client.State;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text.Json;

namespace CRM.Client.Services.Auth
{
    // ✅ USER-DEFINED: Authentication orchestration service
  
    public class AuthService
    {
        private readonly ApiClientService _api;
        private readonly JwtAuthStateProvider _authProvider;
        private readonly AppState _appState;
        private readonly TokenService _tokenService;
        private readonly NavigationManager _navigation;

        public AuthService(
            ApiClientService api,
            JwtAuthStateProvider authProvider,
            AppState appState,
            TokenService tokenService,
            NavigationManager navigationManager)
        {
            _api = api;
            _authProvider = authProvider;
            _appState = appState;
            _tokenService = tokenService;
            _navigation = navigationManager;
        }

        // =====================================================
        // ✅ LOGIN → Calls: POST api/auth/login
        // =====================================================
          public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto)
        {
            // Use PostRawAsync so we can read non-success responses (e.g. password_expired)
            var resp = await _api.PostRawAsync("api/auth/login", dto);

            if (resp == null)
                return null;

            var body = await resp.Content.ReadAsStringAsync();

            // If server indicates password expired (403 + { error: "password_expired" })
            if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                try
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (dict != null && dict.TryGetValue("error", out var code) && code == "password_expired")
                    {
                        // Show global message and redirect user to change-expired-password flow
                        try
                        {
                            _appState.Ui.ShowGlobalMessage("Your password has expired. Please change your password now.", "warning");
                        }
                        catch { /* swallow */ }

                        // Redirect to change-expired-password page (client should implement page)
                        _navigation.NavigateTo("/change-expired-password", true);

                        return null;
                    }
                }
                catch
                {
                    // ignore parse errors - fall through to generic handling
                }

                // For other non-success, return null
                return null;
            }

            if (!resp.IsSuccessStatusCode)
                return null;

            // Parse success response
            AuthResponseDto? response;
            try
            {
                response = JsonSerializer.Deserialize<AuthResponseDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }

            if (response == null)
                return null;

            // ✅ IMPORTANT: If MFA is required, DO NOT expect a token yet
            if (response.MfaRequired)
                return response;

            // ✅ Normal login must have token
            if (string.IsNullOrWhiteSpace(response.Token))
                return null;

            // Update Blazor auth state and persist tokens centrally
            await _authProvider.MarkUserAsAuthenticated(response.Token);
            await _tokenService.SaveTokenAsync(response.Token);

            var authState = await _authProvider.GetAuthenticationStateAsync();
            _appState.Auth.SetAuthenticated(response.Token, authState.User);

            // Save refresh token + expiry for silent refresh
            if (!string.IsNullOrWhiteSpace(response.RefreshToken))
            {
                await _tokenService.SaveRefreshTokenAsync(
                    response.RefreshToken!,
                    response.RefreshExpiresAt
                );
            }

            return response;
        }

        // =====================================================
        // ✅ FINAL MFA LOGIN → POST api/auth/mfa/login
        // =====================================================
        public async Task<AuthResponseDto?> MfaLoginAsync(MfaLoginDto dto)
        {
            var response = await _api.PostAsync<MfaLoginDto, AuthResponseDto>(
                "api/auth/mfa/login", dto);

            if (response == null || string.IsNullOrWhiteSpace(response.Token))
                return null;

            // Update Blazor authentication state and persist tokens centrally
            await _authProvider.MarkUserAsAuthenticated(response.Token);
            await _tokenService.SaveTokenAsync(response.Token);

            var authState = await _authProvider.GetAuthenticationStateAsync();
            _appState.Auth.SetAuthenticated(response.Token, authState.User);

            if (!string.IsNullOrWhiteSpace(response.RefreshToken))
            {
                await _tokenService.SaveRefreshTokenAsync(
                    response.RefreshToken!,
                    response.RefreshExpiresAt
                );
            }

            return response;
        }

        // =====================================================
        // ✅ LOGOUT
        // =====================================================
        public async Task LogoutAsync()
        {
            try
            {
                await _api.PostAsync<object, object>("api/auth/logout", new { });
            }
            catch
            {
                // ignore errors from server-side cleanup
            }

            await _authProvider.MarkUserAsLoggedOut();
            await _tokenService.ClearAsync();
            _appState.Auth.Clear();
        }

        // =====================================================
        // ✅ COMPLETE REGISTRATION → POST api/auth/complete-registration
        // =====================================================
        public async Task<OperationResultDto?> CompleteRegistrationAsync(CompleteRegistrationDto dto)
        {
            var response = await _api.PostAsync<CompleteRegistrationDto, OperationResultDto>(
                "api/account/complete-registration", dto);

            return response;
        }

        // =====================================================
        // ✅ CHANGE PASSWORD → POST api/auth/change-password
        // =====================================================
        public async Task ChangePasswordAsync(ChangePasswordDto dto)
        {
            var response = await _api.PostRawAsync("api/account/change-password", dto);

            if (response.IsSuccessStatusCode)
                return;

            var raw = await response.Content.ReadAsStringAsync();

            // 1️⃣ Try structured JSON error
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                var root = doc.RootElement;

                // Case 1: { "message": "..." }
                if (root.TryGetProperty("message", out var messageProp))
                {
                    var message = messageProp.GetString();
                    if (!string.IsNullOrWhiteSpace(message))
                        throw new Exception(message);
                }

                // Case 2: Validation errors { errors: { Field: [ "msg" ] } }
                if (root.TryGetProperty("errors", out var errors))
                {
                    foreach (var errorField in errors.EnumerateObject())
                    {
                        var msg = errorField.Value[0].GetString();
                        if (!string.IsNullOrWhiteSpace(msg))
                            throw new Exception(msg);
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Not JSON → fall through
            }

            // 3️⃣ Final fallback (only if nothing usable found)
            throw new Exception("Password update failed. Please try again.");
        }





        // =====================================================
        // ✅ FORGOT PASSWORD → POST api/auth/forgot-password
        // =====================================================
        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            await _api.PostAsync<ForgotPasswordDto, object>(
                "api/account/forgot-password", dto);
        }

        // =====================================================
        // ✅ RESET PASSWORD → POST api/auth/reset-password
        // (returns tokens; persist and sign-in)
        // =====================================================
        public async Task<AuthResponseDto?> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var response = await _api.PostAsync<ResetPasswordDto, AuthResponseDto>(
                "api/account/reset-password", dto);

            if (response == null)
                return null;

            // Save access token and update auth state
            if (!string.IsNullOrWhiteSpace(response.Token))
            {
                await _authProvider.MarkUserAsAuthenticated(response.Token);
                await _tokenService.SaveTokenAsync(response.Token);

                var authState = await _authProvider.GetAuthenticationStateAsync();
                _appState.Auth.SetAuthenticated(response.Token, authState.User);
            }

            // Save refresh token if provided
            if (!string.IsNullOrWhiteSpace(response.RefreshToken))
            {
                await _tokenService.SaveRefreshTokenAsync(
                    response.RefreshToken!,
                    response.RefreshExpiresAt
                );
            }

            return response;
        }

        // =====================================================
        // ✅ CHANGE EXPIRED PASSWORD → POST api/auth/change-expired-password
        // (returns tokens; persist and sign-in)
        // =====================================================
        public async Task<AuthResponseDto?> ChangeExpiredPasswordAsync(ChangeExpiredPasswordDto dto)
        {
            var response = await _api.PostRawAsync(
                "api/account/change-expired-password", dto);

            if (response.IsSuccessStatusCode)
            {
                var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

                if (auth == null)
                    return null;

                // ✅ Auto-login (this flow is allowed to do this)
                if (!string.IsNullOrWhiteSpace(auth.Token))
                {
                    await _authProvider.MarkUserAsAuthenticated(auth.Token);
                    await _tokenService.SaveTokenAsync(auth.Token);

                    var authState = await _authProvider.GetAuthenticationStateAsync();
                    _appState.Auth.SetAuthenticated(auth.Token, authState.User);
                }

                if (!string.IsNullOrWhiteSpace(auth.RefreshToken))
                {
                    await _tokenService.SaveRefreshTokenAsync(
                        auth.RefreshToken!,
                        auth.RefreshExpiresAt
                    );
                }

                return auth;
            }

            // ❌ Error handling (same pattern as ChangePassword)
            var raw = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (root.TryGetProperty("message", out var msgProp))
                {
                    var msg = msgProp.GetString();
                    if (!string.IsNullOrWhiteSpace(msg))
                        throw new Exception(msg);
                }

                if (root.TryGetProperty("errors", out var errors))
                {
                    foreach (var prop in errors.EnumerateObject())
                    {
                        var msg = prop.Value[0].GetString();
                        if (!string.IsNullOrWhiteSpace(msg))
                            throw new Exception(msg);
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // not JSON, ignore
            }

            throw new Exception("Password update failed. Please try again.");
        }

        // =====================================================
        // ✅ ENABLE MFA → POST api/auth/mfa/enable
        // =====================================================
        public async Task<EnableMfaResponseDto?> EnableMfaAsync()
        {
            return await _api.PostAsync<object, EnableMfaResponseDto>(
                "api/account/mfa/enable", new { });
        }

        // =====================================================
        // ✅ VERIFY MFA → POST api/auth/mfa/verify
        // =====================================================
        public async Task VerifyMfaAsync(VerifyMfaDto dto)
        {
            await _api.PostAsync<VerifyMfaDto, object>(
                "api/account/mfa/verify", dto);
        }

        // =====================================================
        // ✅ DISABLE MFA → POST api/auth/mfa/disable
        // =====================================================
        public async Task DisableMfaAsync(string code)
        {
            await _api.PostAsync<object, object>(
                "api/account/mfa/disable",
                new { code });
        }

    
    }
}
