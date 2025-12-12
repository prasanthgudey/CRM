using CRM.Client.Components;
using CRM.Client.Config;
using CRM.Client.Security;
using CRM.Client.Services;
using CRM.Client.Services.Audit;
using CRM.Client.Services.Auth;
using CRM.Client.Services.Customers;
using CRM.Client.Services.Http;
using CRM.Client.Services.Roles;
using CRM.Client.Services.Tasks;
using CRM.Client.Services.Users;
using CRM.Client.State;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Bind ApiSettings and register both IOptions<T> and concrete instance
var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() ?? new ApiSettings();
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
builder.Services.AddSingleton(apiSettings);

// 2. HttpClient + ApiClientService
builder.Services.AddScoped<ApiClientService>();

builder.Services.AddHttpClient("ApiClient", (sp, client) =>
{
    var settings = sp.GetRequiredService<
        Microsoft.Extensions.Options.IOptions<ApiSettings>>().Value;

    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});

// 3. Authentication + Authorization Setup (Client-side)
// keep your JwtAuthStateProvider registration for Blazor authentication state
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthStateProvider>()
);

// Custom Role Policy Provider
builder.Services.AddSingleton<IAuthorizationPolicyProvider, RolePolicyProvider>();

// 4. App-wide UI + Auth state
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<UiState>();

// 5. Domain Services (Auth, Users, Roles, Audit)
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<CustomerService>();

//testing global search
builder.Services.AddHttpClient<SearchService>(client =>
{
    // set this to the CRM.Server base URL from your logs.
    // In your logs earlier, server listened at 
    client.BaseAddress = new Uri("https://localhost:7194");
});
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient());

// register your service
builder.Services.AddScoped<SearchService>();



// 6. Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- BEGIN: Minimal server-side authentication/authorization additions ---
// Add server-side authentication so IAuthenticationService exists when Blazor's
// AuthorizeRouteView / [Authorize] logic runs on the server.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // When server-side tries to challenge, redirect to this page
        options.LoginPath = "/please-login";
        options.AccessDeniedPath = "/please-login";
        options.Cookie.Name = "CRM.ServerAuth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Server-side authorization (in addition to AddAuthorizationCore used by Blazor)
builder.Services.AddAuthorization();

// For component-level authorization
builder.Services.AddAuthorizationCore();

// Ensure the Blazor AuthenticationStateProvider (JWT) is still the one used by components
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();
// --- END: additions ---

var app = builder.Build();

// Configure Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// NOTE: removed the earlier misplaced UseAntiforgery() call here (we'll add it in the correct spot)

// IMPORTANT: enable routing BEFORE authentication/authorization middlewares
app.UseRouting();

// Authentication + Authorization middleware — order matters
app.UseAuthentication();
app.UseAuthorization();

// --- CORRECT PLACE for antiforgery middleware ---
// Calls must appear after UseAuthentication/UseAuthorization and between UseRouting() and endpoint mapping.
app.UseAntiforgery();

// Now map endpoints / components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
