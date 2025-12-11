
using CRM.Client.Components;
using CRM.Client.Config;
using CRM.Client.Security;
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


// 6. Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();