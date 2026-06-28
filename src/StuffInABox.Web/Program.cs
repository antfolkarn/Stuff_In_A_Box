using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using StuffInABox.Application.Common.Access;
using StuffInABox.Application.Common.Behaviors;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Infrastructure;
using StuffInABox.Infrastructure.Persistence;
using StuffInABox.Web.Auth;
using StuffInABox.Web.Endpoints;
using StuffInABox.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Structured logging to both console and a rolling daily file
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: context.Configuration["Logging:File:Path"] ?? "logs/stuffinabox-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

// Resolve the uploads directory and keep it OUTSIDE wwwroot — otherwise an SPA
// rebuild (Vite empties wwwroot) would delete user photos. The resolved absolute
// path is written back to config so the storage service and the static-file
// middleware below use exactly the same location.
var uploadsPath = builder.Configuration["Storage:LocalPath"];
if (string.IsNullOrWhiteSpace(uploadsPath))
{
    uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
    builder.Configuration["Storage:LocalPath"] = uploadsPath;
}

// MediatR + FluentValidation
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(StuffInABox.Application.Spaces.Commands.CreateSpace.CreateSpaceCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(EmailVerificationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(StuffInABox.Application.Spaces.Commands.CreateSpace.CreateSpaceCommandValidator).Assembly);

// Infrastructure (EF Core, repositories, storage, tagging)
builder.Services.AddInfrastructure(builder.Configuration);

// Serialize enums as their names (e.g. item EnrichmentStatus -> "Pending"/"Completed")
// so the API contract stays readable instead of leaking integer values.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Auth
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ISpaceAccessService, SpaceAccessService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddHttpClient<OAuthService>();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret must be configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "stuffinabox",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "stuffinabox",
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Rate limiting — protect auth endpoints from brute force, partitioned per client IP
var authPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:AuthPermitLimit") ?? 10;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

// CORS — allow the React dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? ["http://localhost:5173"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Health checks — liveness (/health) and readiness (/health/ready, checks the DB)
builder.Services.AddHealthChecks()
    .AddTypeActivatedCheck<StuffInABox.Web.HealthChecks.DatabaseHealthCheck>(
        "database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: ["ready"]);

// Exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Prepare the database on startup. SQLite (dev/tests) builds the schema straight from
// the model — no migrations to maintain for the throwaway dev DB. Postgres (production)
// applies the committed migrations. Skipped under EF design-time tooling (migrations
// add/scaffold), which builds this host but must not touch a live database.
if (!EF.IsDesignTime)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsSqlite())
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();
}

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

// Uploaded photos are served by MapPhotoEndpoints (signed /uploads/{key}) — not as
// plain static files — so they aren't world-readable. Ensure the directory exists.
Directory.CreateDirectory(uploadsPath);

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// API endpoints
app.MapAuthEndpoints();
app.MapOAuthEndpoints();
app.MapSpaceEndpoints();
app.MapInviteEndpoints();
app.MapBoxEndpoints();
app.MapItemEndpoints();
app.MapPhotoEndpoints();
app.MapSearchEndpoints();
app.MapLabelEndpoints();
app.MapSettingsEndpoints();
app.MapAccountEndpoints();

// Health endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false, // liveness: app is responding, run no checks
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

// SPA fallback — serve index.html for any non-API route (state-driven client routing)
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
