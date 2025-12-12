using System;
using System.Timers;   // ensures correct Timer type

namespace CRM.Client.State
{
    public class UiState
    {
        public bool IsSidebarCollapsed { get; private set; }
        public bool IsPageLoading { get; private set; }

        public string? GlobalMessage { get; private set; }
        public string? GlobalMessageType { get; private set; }  // success, warning, info, danger

        private System.Timers.Timer? _clearTimer;

        public event Action? OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();

        public void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;
        public void ShowLoader() => IsPageLoading = true;
        public void HideLoader() => IsPageLoading = false;

        public void ShowGlobalMessage(string message, string level = "info", int autoClearSeconds = 6)
        {
            GlobalMessage = message;
            GlobalMessageType = level;

            // Stop and dispose of previous timer
            _clearTimer?.Stop();
            _clearTimer?.Dispose();

            if (autoClearSeconds > 0)
            {
                _clearTimer = new System.Timers.Timer(autoClearSeconds * 1000);
                _clearTimer.Elapsed += (s, e) =>
                {
                    ClearGlobalMessage();
                };
                _clearTimer.AutoReset = false;
                _clearTimer.Start();
            }

            NotifyStateChanged();
        }

        public void ClearGlobalMessage()
        {
            GlobalMessage = null;
            GlobalMessageType = null;

            NotifyStateChanged();
        }
    }
}
