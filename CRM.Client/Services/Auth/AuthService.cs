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
            // ✅ Calls your real endpoint
            var response = await _api.PostAsync<LoginRequestDto, AuthResponseDto>(
                "api/auth/login", dto);

            if (response == null || string.IsNullOrWhiteSpace(response.Token))
                return null;

            // ✅ Update Blazor authentication state
            await _authProvider.MarkUserAsAuthenticated(response.Token);

            // ✅ Read claims directly from provider
            var authState = await _authProvider.GetAuthenticationStateAsync();

            // ✅ Sync into global AppState
            _appState.Auth.SetAuthenticated(response.Token, authState.User);

            return response;
        }

        // =====================================================
        // ✅ COMPLETE REGISTRATION → POST api/auth/complete-registration
        // =====================================================
        public async Task<OperationResultDto?> CompleteRegistrationAsync(CompleteRegistrationDto dto)
        {
            var response = await _api.PostAsync<CompleteRegistrationDto, OperationResultDto>(
                "api/auth/complete-registration", dto);   // new code

            return response;                               // new code
        }

        // =====================================================
        // ✅ LOGOUT
        // =====================================================
        public async Task LogoutAsync()
        {
            await _authProvider.MarkUserAsLoggedOut();
            _appState.Auth.Clear();
        }
    }
}
