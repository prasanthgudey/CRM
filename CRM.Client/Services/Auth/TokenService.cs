using CRM.Client.State;
using Microsoft.JSInterop;

namespace CRM.Client.Services.Auth
{
    public class TokenService
    {
        private const string ACCESS_TOKEN_KEY = "authToken";
        private const string REFRESH_TOKEN_KEY = "refreshToken";
        private const string REFRESH_EXPIRES_KEY = "refreshExpiresAt";

        private readonly IJSRuntime _js;
        private readonly AppState _appState;

        public TokenService(IJSRuntime js, AppState appState)
        {
            _js = js;
            _appState = appState;
        }

        // ------------------------
        // ACCESS TOKEN
        // ------------------------
        public async Task SaveTokenAsync(string token)
        {
            if (!string.IsNullOrWhiteSpace(token))
                await _js.InvokeVoidAsync("localStorage.setItem", ACCESS_TOKEN_KEY, token);
        }

        public async Task<string?> GetTokenAsync()
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", ACCESS_TOKEN_KEY);
        }

        public async Task ClearAsync()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", ACCESS_TOKEN_KEY);
            await _js.InvokeVoidAsync("localStorage.removeItem", REFRESH_TOKEN_KEY);
            await _js.InvokeVoidAsync("localStorage.removeItem", REFRESH_EXPIRES_KEY);
            _appState.Auth.Clear();
        }

        public bool IsAuthenticated() => _appState.Auth.IsAuthenticated;
        public string? GetRole() => _appState.Auth.Role;

        // ------------------------
        // REFRESH TOKEN (new)
        // ------------------------
        public async Task SaveRefreshTokenAsync(string refreshToken, DateTime? expiresAtUtc)
        {
            if (!string.IsNullOrWhiteSpace(refreshToken))
                await _js.InvokeVoidAsync("localStorage.setItem", REFRESH_TOKEN_KEY, refreshToken);

            if (expiresAtUtc.HasValue)
                await _js.InvokeVoidAsync("localStorage.setItem", REFRESH_EXPIRES_KEY, expiresAtUtc.Value.ToString("o"));
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", REFRESH_TOKEN_KEY);
        }

        public async Task<DateTime?> GetRefreshExpiresAtAsync()
        {
            var s = await _js.InvokeAsync<string?>("localStorage.getItem", REFRESH_EXPIRES_KEY);
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var d))
                return d;
            return null;
        }
    }
}
