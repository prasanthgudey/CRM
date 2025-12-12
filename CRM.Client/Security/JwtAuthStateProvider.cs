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
        //no anonymous access

        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public JwtAuthStateProvider(IJSRuntime js)
        {
            _js = js;
        }

        // -------------------------
        // UPDATED: prerender-safe GetAuthenticationStateAsync
        // -------------------------
        // Important: we avoid allowing the exception thrown during server prerender
        // to bubble up. If JS interop isn't available (prerender), we return anonymous.
        // The client should call InitializeFromClientAsync() after first render to
        // let this provider read localStorage and notify auth changes.
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Try to read token from localStorage. This will throw InvalidOperationException
                // when called during server prerendering (JS interop not available).
                var token = await _js.InvokeAsync<string>("localStorage.getItem", TokenKey);

                if (string.IsNullOrWhiteSpace(token))
                {
                    return new AuthenticationState(_anonymous);
                }

                var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch (InvalidOperationException)
            {
                // Occurs during prerendering — JS interop not available.
                // Return anonymous; client will initialize later.
                return new AuthenticationState(_anonymous);
            }
            catch (JSException)
            {
                // Any JS error -> treat as anonymous for now.
                return new AuthenticationState(_anonymous);
            }
            catch (Exception)
            {
                // Be defensive: on any unexpected error, return anonymous.
                return new AuthenticationState(_anonymous);
            }
        }

        // -------------------------
        // NEW: call this method from client-only lifecycle (OnAfterRenderAsync) to
        // initialize auth state using localStorage (JS interop is available on client)
        // -------------------------
        public async Task InitializeFromClientAsync()
        {
            try
            {
                var token = await _js.InvokeAsync<string>("localStorage.getItem", TokenKey);

                if (!string.IsNullOrWhiteSpace(token))
                {
                    var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
                    var user = new ClaimsPrincipal(identity);

                    NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
                    return;
                }
            }
            catch (JSException)
            {
                // ignore JS errors and fall through to anonymous
            }
            catch (InvalidOperationException)
            {
                // JS interop not available — shouldn't happen when called from client after first render,
                // but be defensive.
            }
            catch
            {
                // swallow unexpected errors to avoid breaking rendering.
            }

            // No token found or error -> set anonymous
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
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
