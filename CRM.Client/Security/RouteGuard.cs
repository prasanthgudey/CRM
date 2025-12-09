using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace CRM.Client.Security
{
    // ✅ USER-DEFINED route protection component logic
    public class RouteGuard : ComponentBase
    {
        [Inject] public AuthenticationStateProvider AuthProvider { get; set; } = default!;
        [Inject] public NavigationManager Navigation { get; set; } = default!;

        [Parameter] public string? RequiredRole { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            // ✅ Not logged in
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                Navigation.NavigateTo("/login", true);
                return;
            }

            // ✅ Role-based protection
            if (!string.IsNullOrWhiteSpace(RequiredRole))
            {
                if (!user.IsInRole(RequiredRole))
                {
                    Navigation.NavigateTo("/unauthorized", true);
                }
            }
        }
    }
}
