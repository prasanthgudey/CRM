// Program.cs (Blazor WebAssembly standalone) - safer version
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
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Net.Http;
using CRM.Client.Components;
using CRM.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// make sure wwwroot/appsettings.json is loaded (CreateDefault usually does)
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

// bind ApiSettings from configuration (wwwroot/appsettings.json)
var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() ?? new ApiSettings();

// Validate ApiSettings.BaseUrl early with a helpful message
if (string.IsNullOrWhiteSpace(apiSettings.BaseUrl))
{
    // Helpful error: tells you exactly what to fix
    throw new InvalidOperationException(
        "ApiSettings.BaseUrl is not configured in wwwroot/appsettings.json. " +
        "Add a valid absolute URL (including scheme) e.g. \"https://localhost:7194/\"."
    );
}

// Normalize base URL to end with a slash — convenient for relative endpoints
var normalizedBase = apiSettings.BaseUrl.EndsWith("/") ? apiSettings.BaseUrl : apiSettings.BaseUrl + "/";

// register ApiSettings for injection (singleton)
builder.Services.AddSingleton(apiSettings);

// Authorization (AuthorizeView, [Authorize], policies client-side)
builder.Services.AddAuthorizationCore();

// Register a shared HttpClient that points to your API base URL
builder.Services.AddScoped(sp =>
{
    return new HttpClient
    {
        BaseAddress = new Uri(normalizedBase),
        Timeout = TimeSpan.FromSeconds(apiSettings.TimeoutSeconds)
    };
});

// Register services (WASM-friendly)
builder.Services.AddScoped<ApiClientService>();    // custom - wrapper over HttpClient
builder.Services.AddScoped<JwtAuthStateProvider>(); // custom AuthStateProvider (WASM-safe)
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();

builder.Services.AddSingleton<IAuthorizationPolicyProvider, RolePolicyProvider>(); // custom
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<UiState>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<SearchService>();

await builder.Build().RunAsync();
