using System.Security.Claims;

namespace CRM.Client.State
{
    // ✅ USER-DEFINED: Holds authentication-related state for the UI
    public class AuthState
    {
        public bool IsAuthenticated { get; private set; }

        public string? Token { get; private set; }

        public ClaimsPrincipal? User { get; private set; }

        public string? Role { get; private set; }

        public void SetAuthenticated(string token, ClaimsPrincipal user)
        {
            IsAuthenticated = true;
            Token = token;
            User = user;

            Role = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        }

        public void Clear()
        {
            IsAuthenticated = false;
            Token = null;
            User = null;
            Role = null;
        }
    }
}
