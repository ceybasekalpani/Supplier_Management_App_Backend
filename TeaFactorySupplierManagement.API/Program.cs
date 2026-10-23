using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using TeaFactorySupplierManagement.API.Services;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Application.Services;
using TeaFactorySupplierManagement.Domain.Interfaces;
using TeaFactorySupplierManagement.Infrastructure.Data;
using TeaFactorySupplierManagement.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys")));

// Controllers / Swagger / HTTP
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Tea Factory Supplier Management API",
        Version = "v1",
        Description = "API for managing tea factory suppliers, leaf collections, and requests",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Support",
            Email = "support@teafactory.com"
        }
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
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
            Array.Empty<string>()
        }
    });
});

// Database
builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
{
    // Manual transactions are used by the disbursement and delivery-note
    // services. Do not enable SqlServerRetryingExecutionStrategy here because
    // it does not support user-initiated transactions unless the entire
    // transaction is wrapped in CreateExecutionStrategy().ExecuteAsync(...).
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
    }
});

// AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<TeaFactorySupplierManagement.Application.Mappings.MappingProfile>();
}, AppDomain.CurrentDomain.GetAssemblies());

// JWT Authentication
var jwtKey = configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT Key is not configured in appsettings.json");
}

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"],

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers["Token-Expired"] = "true";
                }

                return Task.CompletedTask;
            },

            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    message = "You are not authorized to access this resource"
                });

                await context.Response.WriteAsync(result);
            },

            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    message = "You do not have permission to access this resource"
                });

                await context.Response.WriteAsync(result);
            }
        };
    });

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DashboardAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("DashboardAdmin");
    });
});

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILeafService, LeafService>();
builder.Services.AddScoped<ICashRequestService, CashRequestService>();
builder.Services.AddScoped<IFertilizerRequestService, FertilizerRequestService>();
builder.Services.AddScoped<IItemRequestService, ItemRequestService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IFactorySetupService, FactorySetupService>();
builder.Services.AddScoped<ISupplierDashboardService, SupplierDashboardService>();
builder.Services.AddScoped<IDashboardAdminAuthService, DashboardAdminAuthService>();
builder.Services.AddScoped<IDashboardRequestService, DashboardRequestService>();
builder.Services.AddScoped<IDashboardMetricsService, DashboardMetricsService>();
builder.Services.AddScoped<IFertilizerItemConfigurationService, FertilizerItemConfigurationService>();
builder.Services.AddScoped<IDisbursementService, DisbursementService>();
builder.Services.AddScoped<IDashboardCommunicationService, DashboardCommunicationService>();
builder.Services.AddScoped<IDashboardUserService, DashboardUserService>();
builder.Services.AddScoped<IDashboardPermissionService, DashboardPermissionService>();
builder.Services.AddScoped<IFirebasePushService, FirebasePushService>();
builder.Services.AddScoped<IMobileDeviceTokenService, MobileDeviceTokenService>();
builder.Services.AddScoped<ISupabaseStorageService, SupabaseStorageService>();

// Repositories / Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// CORS
// Keep local origins in appsettings.json and provide the deployed frontend
// origin through the FRONTEND_URL environment variable on Render.
var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

var frontendUrl = configuration["FRONTEND_URL"]
    ?? configuration["FrontendUrl"];

var allowedOrigins = configuredOrigins
    .Append(frontendUrl ?? string.Empty)
    .Append("https://supplierappdashboard.vercel.app")
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddHealthChecks();
builder.Services.AddResponseCaching();

// Rate limiting for auth endpoints susceptible to brute-forcing (login, PIN login, etc.)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tea Factory API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHsts();
}

app.UseCors("AllowReactApp");
app.UseStaticFiles();
app.UseResponseCaching();

// IMPORTANT:
// Do not use custom JwtMiddleware here.
// ASP.NET Core JwtBearer already handles expired tokens and authorization.
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

// Database migration + seed
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var runMigrations = configuration.GetValue("Database:RunMigrations", true);

        if (runMigrations)
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
        else
        {
            logger.LogWarning("Database migrations are disabled by Database:RunMigrations");
        }

        await DbInitializer.InitializeAsync(dbContext);

        logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database");
    }
}

app.Run();

