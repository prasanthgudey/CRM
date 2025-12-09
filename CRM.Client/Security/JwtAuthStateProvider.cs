using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace CRM.Client.Security
{
    // ✅ FRAMEWORK CLASS: AuthenticationStateProvider
    // ✅ USER-DEFINED JWT-based implementation
    public class JwtAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _js;

        private const string TokenKey = "authToken";

        public JwtAuthStateProvider(IJSRuntime js)
        {
            _js = js;
        }

        // ✅ FRAMEWORK OVERRIDE: Called by Blazor to get current auth state
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _js.InvokeAsync<string>("localStorage.getItem", TokenKey);

            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity())
                );
            }

            var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }

        // ✅ USER-DEFINED: Call this AFTER successful login
        public async Task MarkUserAsAuthenticated(string token)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);

            var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(user))
            );
        }

        // ✅ USER-DEFINED: Call this on logout
        public async Task MarkUserAsLoggedOut()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);

            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(anonymous))
            );
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
    }
}
