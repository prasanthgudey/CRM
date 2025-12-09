using CRM.Client.State;

namespace CRM.Client.Services.Auth
{
    // ✅ USER-DEFINED: Token access abstraction
    public class TokenService
    {
        private readonly AppState _appState;

        public TokenService(AppState appState)
        {
            _appState = appState;
        }

        public string? GetToken()
        {
            return _appState.Auth.Token;
        }

        public bool IsAuthenticated()
        {
            return _appState.Auth.IsAuthenticated;
        }

        public string? GetRole()
        {
            return _appState.Auth.Role;
        }

        public void Clear()
        {
            _appState.Auth.Clear();
        }
    }
}
