using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;


namespace CRM.Client.Security
{
    /// <summary>
    /// WASM-safe AuthenticationStateProvider.
    /// - Does NOT use System.IdentityModel.* (not available in WASM).
    /// - Parses JWT payload to extract claims; does NOT validate signature.
    /// - Reads/writes tokens from localStorage via IJSRuntime.
    /// </summary>
    public class JwtAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _js;
        //private readonly IHttpClientFactory _httpFactory;
        private readonly HttpClient _http;


        private const string TokenKey = "authToken";
        private const string RefreshTokenKey = "refreshToken";

        private string? _cachedAccessToken;
        private string? _cachedRefreshToken;
        private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

        public JwtAuthStateProvider(IJSRuntime js, HttpClient http)
        {
            _js = js;
            _http = http;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? token = null;

            try
            {
                token = await _js.InvokeAsync<string>("localStorage.getItem", TokenKey);
            }
            catch
            {
                // JS unavailable (prerender or error) -> return anonymous
                ClearHttpClientAuthHeader();
                return new AuthenticationState(Anonymous);
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                ClearHttpClientAuthHeader();
                return new AuthenticationState(Anonymous);
            }

            if (IsJwtExpired(token))
            {
                try { await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey); } catch { }
                ClearHttpClientAuthHeader();
                return new AuthenticationState(Anonymous);
            }

            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
            var user = new ClaimsPrincipal(identity);

            SetHttpClientAuthHeader(token);

            _cachedAccessToken = token;
            try { _cachedRefreshToken = await _js.InvokeAsync<string>("localStorage.getItem", RefreshTokenKey); } catch { }

            return new AuthenticationState(user);
        }

        /// <summary>
        /// Call from a client-only component when JS is available to initialize state.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                var token = await _js.InvokeAsync<string>("localStorage.getItem", TokenKey);
                var refresh = await _js.InvokeAsync<string>("localStorage.getItem", RefreshTokenKey);

                if (!string.IsNullOrWhiteSpace(token) && !IsJwtExpired(token))
                {
                    _cachedAccessToken = token;
                    _cachedRefreshToken = refresh;
                    SetHttpClientAuthHeader(token);

                    var principal = BuildClaimsPrincipal(token);
                    NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
                    return;
                }
            }
            catch
            {
                // ignore JS errors and remain anonymous
            }

            _cachedAccessToken = null;
            _cachedRefreshToken = null;
            ClearHttpClientAuthHeader();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
        }

        public async Task MarkUserAsAuthenticated(string token)
        {
            try { await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token); } catch { /* ignore */ }

            _cachedAccessToken = token;
            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
            var user = new ClaimsPrincipal(identity);

            SetHttpClientAuthHeader(token);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public async Task MarkUserAsLoggedOut()
        {
            try
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
                await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
            }
            catch { /* ignore */ }

            _cachedAccessToken = null;
            _cachedRefreshToken = null;
            ClearHttpClientAuthHeader();

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
        }

        public ClaimsPrincipal BuildClaimsPrincipal(string token)
        {
            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
            return new ClaimsPrincipal(identity);
        }

        #region JWT parsing (WASM-safe)

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            try
            {
                var payloadJson = GetPayloadJson(jwt);
                if (string.IsNullOrEmpty(payloadJson))
                    return Array.Empty<Claim>();

                var dictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
                if (dictionary == null) return Array.Empty<Claim>();

                var claims = new List<Claim>();

                foreach (var kvp in dictionary)
                {
                    var claimType = kvp.Key;

                    // Handle common claim value types gracefully
                    if (kvp.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in kvp.Value.EnumerateArray())
                        {
                            claims.Add(new Claim(claimType, item.ToString()));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(claimType, kvp.Value.ToString()));
                    }
                }

                // If there is no 'sub' claim but 'nameid' exists, prefer standard mapping
                if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier) &&
                    claims.Any(c => c.Type == "sub"))
                {
                    var sub = claims.First(c => c.Type == "sub").Value;
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
                }

                return claims;
            }
            catch
            {
                return Array.Empty<Claim>();
            }
        }

        private string? GetPayloadJson(string jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt)) return null;

            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;

            var payload = parts[1];
            var bytes = ParseBase64WithoutPadding(payload);
            if (bytes == null) return null;

            return Encoding.UTF8.GetString(bytes);
        }

        private byte[]? ParseBase64WithoutPadding(string base64)
        {
            // Add padding if necessary
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            try
            {
                return Convert.FromBase64String(base64);
            }
            catch
            {
                return null;
            }
        }

        private bool IsJwtExpired(string jwt)
        {
            try
            {
                var payloadJson = GetPayloadJson(jwt);
                if (string.IsNullOrEmpty(payloadJson)) return true;

                using var doc = JsonDocument.Parse(payloadJson);
                if (!doc.RootElement.TryGetProperty("exp", out var expElement)) return true;

                // exp is typically a numeric Unix epoch (seconds)
                if (expElement.ValueKind == JsonValueKind.Number && expElement.TryGetInt64(out var expSeconds))
                {
                    var expTime = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
                    return expTime <= DateTime.UtcNow;
                }

                // if exp is string, try parse
                var expString = expElement.ToString();
                if (long.TryParse(expString, out var expParsed))
                {
                    var expTime = DateTimeOffset.FromUnixTimeSeconds(expParsed).UtcDateTime;
                    return expTime <= DateTime.UtcNow;
                }

                return true;
            }
            catch
            {
                return true; // treat invalid token as expired
            }
        }

        #endregion

        #region HttpClient header helpers

        private void SetHttpClientAuthHeader(string jwt)
        {
            try
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);
            }
            catch { }
        }

        private void ClearHttpClientAuthHeader()
        {
            try
            {
                _http.DefaultRequestHeaders.Authorization = null;
            }
            catch { }
        }


        #endregion
    }
}