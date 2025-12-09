namespace CRM.Client.State
{
    // ✅ USER-DEFINED: Root global application state
    public class AppState
    {
        public AuthState Auth { get; } = new AuthState();

        public UiState Ui { get; } = new UiState();
    }
}
