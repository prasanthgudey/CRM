using CRM.Client.DTOs.Auth;
using CRM.Client.Security;
using CRM.Client.Services.Http;
using CRM.Client.State;

namespace CRM.Client.Services.Auth
{
    // ✅ USER-DEFINED: Authentication orchestration service
    public class AuthService
    {
        private readonly ApiClientService _api;
        private readonly JwtAuthStateProvider _authProvider;
        private readonly AppState _appState;

        public AuthService(
            ApiClientService api,
            JwtAuthStateProvider authProvider,
            AppState appState)
        {
            _api = api;
            _authProvider = authProvider;
            _appState = appState;
        }

        // =====================================================
        // ✅ LOGIN → Calls: POST api/auth/login
        // =====================================================
        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto)
        {
            var response = await _api.PostAsync<LoginRequestDto, AuthResponseDto>(
                "api/auth/login", dto);

            if (response == null)
                return null;

            // ✅ IMPORTANT: If MFA is required, DO NOT expect a token yet
            if (response.MfaRequired)
                return response;

            // ✅ Normal login must have token
            if (string.IsNullOrWhiteSpace(response.Token))
                return null;

            await _authProvider.MarkUserAsAuthenticated(response.Token);

            var authState = await _authProvider.GetAuthenticationStateAsync();
            _appState.Auth.SetAuthenticated(response.Token, authState.User);

            return response;
        }


        // =====================================================
        // ✅ COMPLETE REGISTRATION → POST api/auth/complete-registration
        // =====================================================
        public async Task<string?> CompleteRegistrationAsync(CompleteRegistrationDto dto)
        {
            // ✅ Your backend returns plain string
            var response = await _api.PostAsync<CompleteRegistrationDto, string>(
                "api/auth/complete-registration", dto);

            return response;
        }

        // =====================================================
        // ✅ LOGOUT
        // =====================================================
        public async Task LogoutAsync()
        {
            await _authProvider.MarkUserAsLoggedOut();
            _appState.Auth.Clear();
        }


        // =====================================================
        // ✅ CHANGE PASSWORD → POST api/auth/change-password
        // =====================================================
        public async Task ChangePasswordAsync(ChangePasswordDto dto)
        {
            await _api.PostAsync<ChangePasswordDto, object>(
                "api/auth/change-password", dto);
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
        // =====================================================
        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            await _api.PostAsync<ResetPasswordDto, object>(
                "api/auth/reset-password", dto);
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

            // ✅ Update Blazor authentication state
            await _authProvider.MarkUserAsAuthenticated(response.Token);

            var authState = await _authProvider.GetAuthenticationStateAsync();

            _appState.Auth.SetAuthenticated(response.Token, authState.User);

            return response;
        }



    }
}
