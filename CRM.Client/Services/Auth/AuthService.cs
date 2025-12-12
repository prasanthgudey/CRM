using CRM.Client.DTOs.Auth;
using CRM.Client.DTOs.Users;
using CRM.Client.Security;
using CRM.Client.Services.Http;
using CRM.Client.State;
using Microsoft.AspNetCore.Components;
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
        // ✅ COMPLETE REGISTRATION → POST api/auth/complete-registration
        // =====================================================
        public async Task<OperationResultDto?> CompleteRegistrationAsync(CompleteRegistrationDto dto)
        {
            var response = await _api.PostAsync<CompleteRegistrationDto, OperationResultDto>(
                "api/auth/complete-registration", dto);

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
        // ✅ CHANGE PASSWORD → POST api/auth/change-password
        // =====================================================
        public async Task ChangePasswordAsync(ChangePasswordDto dto)
        {
            var response = await _api.PostRawAsync("api/auth/change-password", dto);

            if (response.IsSuccessStatusCode)
                return;

            var raw = await response.Content.ReadAsStringAsync();

            // Try to extract clean message
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(raw);

                // Case 1: Standard { message: "..." }
                if (json.RootElement.TryGetProperty("message", out var msgProp))
                    throw new Exception(msgProp.GetString()!);

                // Case 2: ASP.NET Validation error: errors -> field -> message
                if (json.RootElement.TryGetProperty("errors", out var errors))
                {
                    foreach (var prop in errors.EnumerateObject())
                    {
                        var msg = prop.Value[0].GetString();
                        if (!string.IsNullOrWhiteSpace(msg))
                            throw new Exception(msg!);
                    }
                }

                throw new Exception("Password update failed. Check input.");
            }
            catch
            {
                // fallback: raw message
                throw new Exception("Password update failed. Please try again.");
            }
        }




        // =====================================================
        // ✅ FORGOT PASSWORD → POST api/auth/forgot-password
        // =====================================================
        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            await _api.PostAsync<ForgotPasswordDto, object>(
                "api/auth/forgot-password", dto);
        }

        // =====================================================
        // ✅ RESET PASSWORD → POST api/auth/reset-password
        // (returns tokens; persist and sign-in)
        // =====================================================
        public async Task<AuthResponseDto?> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var response = await _api.PostAsync<ResetPasswordDto, AuthResponseDto>(
                "api/auth/reset-password", dto);

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
            var response = await _api.PostAsync<ChangeExpiredPasswordDto, AuthResponseDto>(
                "api/auth/change-expired-password", dto);

            if (response == null)
                return null;

            if (!string.IsNullOrWhiteSpace(response.Token))
            {
                await _authProvider.MarkUserAsAuthenticated(response.Token);
                await _tokenService.SaveTokenAsync(response.Token);

                var authState = await _authProvider.GetAuthenticationStateAsync();
                _appState.Auth.SetAuthenticated(response.Token, authState.User);
            }

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
        // ✅ ENABLE MFA → POST api/auth/mfa/enable
        // =====================================================
        public async Task<EnableMfaResponseDto?> EnableMfaAsync()
        {
            return await _api.PostAsync<object, EnableMfaResponseDto>(
                "api/auth/mfa/enable", new { });
        }

        // =====================================================
        // ✅ VERIFY MFA → POST api/auth/mfa/verify
        // =====================================================
        public async Task VerifyMfaAsync(VerifyMfaDto dto)
        {
            await _api.PostAsync<VerifyMfaDto, object>(
                "api/auth/mfa/verify", dto);
        }

        // =====================================================
        // ✅ DISABLE MFA → POST api/auth/mfa/disable
        // =====================================================
        public async Task DisableMfaAsync(string code)
        {
            await _api.PostAsync<object, object>(
                "api/auth/mfa/disable",
                new { code });
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
    }
}
