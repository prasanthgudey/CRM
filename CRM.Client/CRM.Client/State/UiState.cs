namespace CRM.Client.State
{
    // ✅ USER-DEFINED: Global UI state container
    public class UiState
    {
        public bool IsSidebarCollapsed { get; private set; }

        public bool IsPageLoading { get; private set; }

        public void ToggleSidebar()
        {
            IsSidebarCollapsed = !IsSidebarCollapsed;
        }

        public void ShowLoader()
        {
            IsPageLoading = true;
        }

        public void HideLoader()
        {
            IsPageLoading = false;
        }
    }
}
