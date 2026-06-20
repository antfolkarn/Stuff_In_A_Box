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

// MediatR + FluentValidation
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(StuffInABox.Application.Spaces.Commands.CreateSpace.CreateSpaceCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(StuffInABox.Application.Spaces.Commands.CreateSpace.CreateSpaceCommandValidator).Assembly);

// Infrastructure (EF Core, repositories, storage, tagging)
builder.Services.AddInfrastructure(builder.Configuration);

// Auth
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
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

// Migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// API endpoints
app.MapAuthEndpoints();
app.MapOAuthEndpoints();
app.MapSpaceEndpoints();
app.MapBoxEndpoints();
app.MapItemEndpoints();
app.MapRecognitionEndpoints();
app.MapSearchEndpoints();
app.MapLabelEndpoints();

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
