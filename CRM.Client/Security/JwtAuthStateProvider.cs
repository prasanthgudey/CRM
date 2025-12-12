using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace CRM.Client.Security
{
    // ✅ FRAMEWORK CLASS: AuthenticationStateProvider
    // ✅ USER-DEFINED JWT-based implementation
    public class JwtAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _js;
        private readonly IHttpClientFactory _httpFactory;

        // standardized keys
        private const string AccessTokenKey = "access_token";
        private const string RefreshTokenKey = "refresh_token"; // keep if you store refresh token client-side

        public JwtAuthStateProvider(IJSRuntime js, IHttpClientFactory httpFactory)
        {
            _js = js;
            _httpFactory = httpFactory;
        }

        // ✅ FRAMEWORK OVERRIDE: Called by Blazor to get current auth state
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _js.InvokeAsync<string>("localStorage.getItem", AccessTokenKey);

            if (string.IsNullOrWhiteSpace(token))
            {
                // ensure HttpClient has no header
                ClearHttpClientAuthHeader();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // If token expired, treat as not authenticated
            if (IsJwtExpired(token))
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
                ClearHttpClientAuthHeader();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
            var user = new ClaimsPrincipal(identity);

            // ensure HttpClient uses this token for outgoing requests
            SetHttpClientAuthHeader(token);

            return new AuthenticationState(user);
        }

        // ✅ USER-DEFINED: Call this AFTER successful login or refresh
        public async Task MarkUserAsAuthenticated(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            await _js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, token);

            var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
            var user = new ClaimsPrincipal(identity);

            // set Authorization header on named client
            SetHttpClientAuthHeader(token);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        // ✅ USER-DEFINED: Call this on logout
        public async Task MarkUserAsLoggedOut()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);

            ClearHttpClientAuthHeader();

            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
        }

        // ✅ USER-DEFINED: Used by ApiClientService to rebuild ClaimsPrincipal from token
        public ClaimsPrincipal BuildClaimsPrincipal(string token)
        {
            var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
            return new ClaimsPrincipal(identity);
        }

        // ✅ USER-DEFINED: Decodes JWT and extracts claims
        private IEnumerable<Claim> ParseClaims(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            return token.Claims;
        }

        // --- Helper: check exp claim to avoid returning expired principal
        private bool IsJwtExpired(string jwt)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);
                var expClaim = token.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
                if (expClaim == null) return false;

                // exp is seconds since epoch
                if (!long.TryParse(expClaim, out var seconds)) return false;
                var exp = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
                return exp <= DateTime.UtcNow;
            }
            catch
            {
                // If token can't be parsed treat as expired/invalid
                return true;
            }
        }

        // --- Helper: set Authorization header on named ApiClient so outgoing requests use it
        private void SetHttpClientAuthHeader(string jwt)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiClient");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            }
            catch
            {
                // swallow — if named client not registered this will fail; we'll handle elsewhere
            }
        }

        // --- Helper: clear auth header
        private void ClearHttpClientAuthHeader()
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiClient");
                client.DefaultRequestHeaders.Authorization = null;
            }
            catch
            {
                // swallow
            }
        }
    }
}
