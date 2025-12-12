using CRM.Client.State;
using Microsoft.JSInterop;

namespace CRM.Client.Services.Auth
{
    // ✅ USER-DEFINED: Token persistence + access layer
    public class TokenService
    {
        private const string TOKEN_KEY = "authToken";

        private readonly IJSRuntime _js;
        private readonly AppState _appState;

        public TokenService(IJSRuntime js, AppState appState)
        {
            _js = js;
            _appState = appState;
        }

        // ✅ SAVE TOKEN to localStorage + memory
        public async Task SaveTokenAsync(string token)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, token);
            _appState.Auth.SetAuthenticated(token, _appState.Auth.User!);
        }

        // ✅ READ TOKEN from localStorage
        public async Task<string?> GetTokenAsync()
        {
            try
            {
                // localStorage call — may throw during prerender
                return await _js.InvokeAsync<string?>("localStorage.getItem", TOKEN_KEY);
            }
            catch (InvalidOperationException) // JS interop not available (prerender)
            {
                return null;
            }
            catch (JSException)
            {
                // optionally log then return null
                return null;
            }
        }

        // ✅ CLEAR TOKEN everywhere
        public async Task ClearAsync()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
            _appState.Auth.Clear();
        }

        // ✅ SAFE Auth check
        public bool IsAuthenticated()
        {
            return _appState.Auth.IsAuthenticated;
        }

        public string? GetRole()
        {
            return _appState.Auth.Role;
        }
    }
}
