using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace CRM.Client.Security
{
    public class JwtAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _js;
        private readonly IHttpClientFactory _httpFactory;

        private const string TokenKey = "authToken";   // your existing key

        public JwtAuthStateProvider(IJSRuntime js, IHttpClientFactory httpFactory)
        {
            _js = js;
            _httpFactory = httpFactory;
        }

        // Called by Blazor to get current auth state
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _js.InvokeAsync<string>("localStorage.getItem", TokenKey);

            if (string.IsNullOrWhiteSpace(token))
            {
                ClearHttpClientAuthHeader();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // If expired, treat as logged out
            if (IsJwtExpired(token))
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
                ClearHttpClientAuthHeader();

                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
            var user = new ClaimsPrincipal(identity);

            SetHttpClientAuthHeader(token);

            return new AuthenticationState(user);
        }

        // After login OR after refresh
        public async Task MarkUserAsAuthenticated(string token)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);

            var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
            var user = new ClaimsPrincipal(identity);

            SetHttpClientAuthHeader(token);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        // On logout
        public async Task MarkUserAsLoggedOut()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);

            ClearHttpClientAuthHeader();

            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
        }

        // Helper: parse claims
        // ✅ USER-DEFINED: Used by ApiClientService to rebuild ClaimsPrincipal from token
        public ClaimsPrincipal BuildClaimsPrincipal(string token)
        {
            var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
            return new ClaimsPrincipal(identity);
        }

        private IEnumerable<Claim> ParseClaims(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            return token.Claims;
        }

        // Helper: expiration check
        private bool IsJwtExpired(string jwt)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);

                var exp = token.Claims.FirstOrDefault(x => x.Type == "exp")?.Value;
                if (exp == null) return false;

                var expTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)).UtcDateTime;

                return expTime <= DateTime.UtcNow;
            }
            catch
            {
                return true; // treat invalid token as expired
            }
        }

        // Update HttpClient header
        private void SetHttpClientAuthHeader(string jwt)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiClient");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);
            }
            catch
            {
                // ignore if client not registered yet
            }
        }

        private void ClearHttpClientAuthHeader()
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiClient");
                client.DefaultRequestHeaders.Authorization = null;
            }
            catch { }
        }
    }
}
