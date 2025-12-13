
using CRM.Server.Data;
using CRM.Server.Middleware;
using CRM.Server.Models;
using CRM.Server.Repositories;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Repositories.Tasks;
using CRM.Server.Security;
using CRM.Server.Services;
using CRM.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);



// =========================
// ✅ ADD SERVICES TO CONTAINER
// =========================

// ✅ Controllers
builder.Services.AddControllers();

// ✅ Swagger with JWT Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CRM API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter token like this: Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});



// =========================
// ✅ ✅ ✅ CORS CONFIG (MUST BE BEFORE BUILD)
// =========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev", policy =>
    {
        policy.WithOrigins("https://localhost:7149")   // ✅ Blazor Client URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});



// =========================
// ✅ DATABASE CONFIGURATION (WITH RETRY ✅)
// =========================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }
    )
);




// =========================
// ✅ IDENTITY + ROLES CONFIG
// =========================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // ✅ PASSWORD POLICY
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;

    // ✅ USER SETTINGS
    options.User.RequireUniqueEmail = true;

    // ✅ SIGN-IN SETTINGS (CRITICAL FOR MFA)
    options.SignIn.RequireConfirmedEmail = false;   // you are not using email confirmation
    options.SignIn.RequireConfirmedPhoneNumber = false;

    // ✅ MFA SETTINGS (VERY IMPORTANT)
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
    options.Tokens.ProviderMap[TokenOptions.DefaultAuthenticatorProvider] =
        new TokenProviderDescriptor(typeof(AuthenticatorTokenProvider<ApplicationUser>));
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders(); // ✅ REQUIRED for MFA



// =========================
// ✅ JWT SETTINGS BINDING
// =========================
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

builder.Services.Configure<PasswordPolicySettings>(
    builder.Configuration.GetSection("PasswordPolicySettings"));

builder.Services.Configure<MfaSettings>(
    builder.Configuration.GetSection("MfaSettings"));

// Program.cs (before building the app)
builder.Services.Configure<PasswordPolicySettings>(
    builder.Configuration.GetSection("PasswordPolicySettings"));

builder.Services.Configure<RefreshTokenSettings>(
    builder.Configuration.GetSection("RefreshTokenSettings"));

// ... earlier in Program.cs where you configure options:
builder.Services.Configure<SessionSettings>(
    builder.Configuration.GetSection("SessionSettings"));


// =========================
// ✅ JWT AUTHENTICATION
// =========================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
        ),

        // IMPORTANT: reduce/disable default clock skew for strict expiry (use TimeSpan.Zero for testing)
        ClockSkew = TimeSpan.Zero
    };
});

// 🔹 1. Configure Serilog FIRST
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)   // reads "Serilog" from appsettings.json
    .Enrich.FromLogContext()
    .CreateLogger();

// 🔹 2. Tell ASP.NET Core to use Serilog
builder.Host.UseSerilog();

// =========================
// ✅ DEPENDENCY INJECTION
// =========================

// ✅ Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ISearchRepository, SearchRepository>();
builder.Services.AddScoped<ISearchService, SearchService>();



// ✅ Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();


// =========================
// ✅ BUILD APPLICATION
// =========================
var app = builder.Build();



// =========================
// ✅ ROLE SEEDING (Admin, Manager, User)
// =========================
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    await RoleSeedData.SeedRolesAsync(roleManager);
    await UsersSeedData.SeedUsersAsync(userManager);
}



// =========================
// ✅ MIDDLEWARE PIPELINE
// =========================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// GLOBAL EXCEPTION HANDLING - place early so it can catch downstream errors
//app.UseMiddleware<GlobalExceptionMiddleware>();

// ROUTING must come before anything that uses endpoint metadata (context.GetEndpoint)
app.UseRouting();

// CORS when using endpoint routing: between UseRouting and endpoint execution
app.UseCors("LocalDev");

// Authentication populates HttpContext.User (required by SessionActivityMiddleware)
app.UseAuthentication();

// SessionActivityMiddleware needs:
//  - endpoint metadata (so it must run after UseRouting)
//  - an authenticated HttpContext.User (so it must run after UseAuthentication)
// Place it BEFORE UseAuthorization so it can deny/revoke sessions before policy enforcement.
app.UseMiddleware<SessionActivityMiddleware>();

// Authorization runs after session checks, so policies and [Authorize] are enforced on validated sessions
app.UseAuthorization();

// Map controllers / endpoints last
app.MapControllers();

app.Run();

