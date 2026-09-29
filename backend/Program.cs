using System.Threading.RateLimiting;
using AdvancedOrderSystem.Auth;
using AdvancedOrderSystem.Data;
using AdvancedOrderSystem.Hubs;
using AdvancedOrderSystem.Middleware;
using AdvancedOrderSystem.Models.Entities;
using AdvancedOrderSystem.Repositories;
using AdvancedOrderSystem.Repositories.Impl;
using AdvancedOrderSystem.Services;
using AdvancedOrderSystem.Services.Impl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

const string FrontendCorsPolicy = "Frontend";

// Load .env into environment variables before configuration is built.
// NoClobber keeps variables that are already set in the real environment.
DotNetEnv.Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSignalR();

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// =========================
// Options
// =========================

builder.Services.Configure<AuthOptions>(
    builder.Configuration.GetSection(AuthOptions.SectionName));

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));

builder.Services.Configure<FrontendOptions>(
    builder.Configuration.GetSection(FrontendOptions.SectionName));

var googleOptions =
    builder.Configuration
        .GetSection(AdvancedOrderSystem.Auth.GoogleOptions.SectionName)
        .Get<AdvancedOrderSystem.Auth.GoogleOptions>()
    ?? new AdvancedOrderSystem.Auth.GoogleOptions();

var authOptions =
    builder.Configuration
        .GetSection(AuthOptions.SectionName)
        .Get<AuthOptions>() ?? new AuthOptions();

var emailOptions =
    builder.Configuration
        .GetSection(EmailOptions.SectionName)
        .Get<EmailOptions>() ?? new EmailOptions();

// =========================
// CORS
// =========================

var allowedOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Needed so the browser sends the auth cookie
            .AllowCredentials();
    });
});

// =========================
// Database
// =========================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});

// =========================
// Identity
// =========================

builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = authOptions.MaxFailedLoginAttempts;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(authOptions.LockoutMinutes);

        options.SignIn.RequireConfirmedEmail = true;

        // Password reset links expire sooner than the other tokens
        options.Tokens.PasswordResetTokenProvider = AuthConstants.PasswordResetTokenProvider;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddTokenProvider<PasswordResetTokenProvider<AppUser>>(
        AuthConstants.PasswordResetTokenProvider);

// Signs sessions out when the password changes or two-factor is turned off
builder.Services.AddScoped<ISecurityStampValidator, SecurityStampValidator<AppUser>>();
builder.Services.AddScoped<
    ITwoFactorSecurityStampValidator, TwoFactorSecurityStampValidator<AppUser>>();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(5);
});

// =========================
// Authentication cookies
// =========================

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = AuthConstants.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.ExpireTimeSpan = AuthConstants.SessionDuration;
        options.SlidingExpiration = true;

        options.Events.OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync;

        // An API answers with status codes instead of redirecting to a login page
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    })
    // Holds the half-finished sign-in between the password and the 2FA code
    .AddCookie(IdentityConstants.TwoFactorUserIdScheme, options =>
    {
        options.Cookie.Name = AuthConstants.TwoFactorUserIdCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.ExpireTimeSpan = AuthConstants.TwoFactorWindow;
    })
    // Remembers a device so 2FA is not asked for on every sign-in
    .AddCookie(IdentityConstants.TwoFactorRememberMeScheme, options =>
    {
        options.Cookie.Name = AuthConstants.TwoFactorRememberMeCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.ExpireTimeSpan = AuthConstants.TwoFactorRememberMeDuration;

        options.Events.OnValidatePrincipal =
            SecurityStampValidator.ValidateAsync<ITwoFactorSecurityStampValidator>;
    })
    // Not used yet; kept so external sign-in (and SignOutAsync) work
    .AddCookie(IdentityConstants.ExternalScheme, options =>
    {
        options.Cookie.Name = AuthConstants.ExternalCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });

// Customers can sign in with Google when credentials are configured
if (googleOptions.IsConfigured)
{
    builder.Services
        .AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleOptions.ClientId;
            options.ClientSecret = googleOptions.ClientSecret;
            // The external cookie holds the Google identity until we sign the user in
            options.SignInScheme = IdentityConstants.ExternalScheme;
        });
}

// =========================
// Authorization
// =========================

var adminPolicy =
    new AuthorizationPolicyBuilder(IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser()
        .RequireRole(AuthConstants.AdminRole)
        .Build();

var customerPolicy =
    new AuthorizationPolicyBuilder(IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser()
        .RequireRole(AuthConstants.CustomerRole)
        .Build();

var signedInPolicy =
    new AuthorizationPolicyBuilder(IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser()
        .RequireRole(AuthConstants.AllRoles)
        .Build();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthConstants.AdminPolicy, adminPolicy)
    .AddPolicy(AuthConstants.CustomerPolicy, customerPolicy)
    .AddPolicy(AuthConstants.SignedInPolicy, signedInPolicy)
    // Endpoints must opt in to a role; anything unmarked needs an admin
    .SetFallbackPolicy(adminPolicy);

// =========================
// Rate limiting
// =========================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Sign-in / sign-up: 10 requests per minute per IP address
    options.AddPolicy(AuthConstants.AuthRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many attempts. Please wait a minute and try again." },
            cancellationToken);
    };
});

// =========================
// Application services
// =========================

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentCustomerService, CurrentCustomerService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

// Falls back to logging the email when SMTP is not configured
if (emailOptions.IsSmtpConfigured)
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
}

var app = builder.Build();

app.UseExceptionHandler();

// CORS runs before the HTTPS redirect and auth so preflight requests succeed
app.UseCors(FrontendCorsPolicy);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.MapControllers();

app.MapHub<NotificationHub>("/hubs/notifications")
    .RequireAuthorization(AuthConstants.SignedInPolicy);

await EnsureRolesExistAsync(app);

app.Run();

// The roles have to exist before the first account can be added to one
static async Task EnsureRolesExistAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (!await context.Database.CanConnectAsync())
    {
        logger.LogWarning(
            "Database not reachable - run 'dotnet ef database update' before signing in.");

        return;
    }

    var roleManager =
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

    foreach (var role in AuthConstants.AllRoles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));

            logger.LogInformation("Created the {Role} role", role);
        }
    }
}
