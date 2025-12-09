using CRM.Client.Components;
using CRM.Client.Services.Audit;
using CRM.Client.Services.Auth;
using CRM.Client.Services.Http;
using CRM.Client.Services.Roles;
using CRM.Client.Services.Users;
using CRM.Client.State;
using CRM.Client.Config;
using CRM.Client.Security;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);



// ======================================================
// ✅ CONFIGURATION (ApiSettings)
// ======================================================
var apiSettingsSection = builder.Configuration.GetSection("ApiSettings");
var apiSettings = apiSettingsSection.Get<ApiSettings>() ?? new ApiSettings();
builder.Services.AddSingleton(apiSettings);



// ======================================================
// ✅ BLAZOR + WASM SETUP
// ======================================================
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();



// ======================================================
// ✅ AUTH & SECURITY
// ======================================================
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthStateProvider>());



// ======================================================
// ✅ STATE MANAGEMENT
// ======================================================
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<UiState>();



// ======================================================
// ✅ CORE HTTP LAYER
// ======================================================
builder.Services.AddScoped<ApiClientService>();

builder.Services.AddScoped(sp =>
{
    Uri? baseUri = Uri.TryCreate(apiSettings.BaseUrl, UriKind.Absolute, out var u)
        ? u
        : null;

    var client = baseUri is null
        ? new HttpClient()
        : new HttpClient { BaseAddress = baseUri };

    client.Timeout = TimeSpan.FromSeconds(apiSettings.TimeoutSeconds);
    return client;
});



// ======================================================
// ✅ BUSINESS SERVICES
// ======================================================
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<AuditService>();



// ======================================================
// ✅ BUILD APP
// ======================================================
var app = builder.Build();



// ======================================================
// ✅ MIDDLEWARE PIPELINE (REQUIRED)
// ======================================================
app.UseStaticFiles();
app.UseAntiforgery();



// ======================================================
// ✅ MAP BLAZOR HOST (THIS PREVENTS AUTO SHUTDOWN)
// ======================================================
app.MapRazorComponents<App>()
   .AddInteractiveWebAssemblyRenderMode();



// ======================================================
// ✅ RUN SERVER
// ======================================================
app.Run();
