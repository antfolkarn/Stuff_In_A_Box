using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
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

// Load user-secrets explicitly (by the Program assembly) in Development. The automatic load
// resolves the secret store via env.ApplicationName, which in some run configurations doesn't
// match this assembly — leaving OAuth client ids empty (→ #error=oauth_not_configured). Binding
// to the concrete type is deterministic. Dev only; production uses Key Vault references.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);

    // Belt-and-braces: AddUserSecrets resolves the store from the APPDATA environment
    // variable, which a launcher (e.g. VS's debug host) can hand down corrupted — making the
    // whole store "missing" for that process tree. The known-folder API asks Windows instead
    // of trusting the inherited variable, so load the same file via that path too.
    var knownFolderStore = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Microsoft", "UserSecrets", "2d43a745-3dac-489a-9470-798817153c40", "secrets.json");
    builder.Configuration.AddJsonFile(knownFolderStore, optional: true, reloadOnChange: false);

    // Last-resort dev store: a git-ignored file in the project folder, for machines where the
    // profile UserSecrets folder is unreadable from the IDE-spawned process (observed: the VS
    // debug host got DirectoryNotFound on an existing store — endpoint security suspected).
    // The project folder is always readable (appsettings.json comes from it).
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
}

// Structured logging to both console (captured by App Service's log stream in Azure) and a
// rolling daily file. Retention + size are capped and configurable so the file sink can't fill
// the App Service /home quota — defaults keep only the current day and roll at a size ceiling.
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: context.Configuration["Logging:File:Path"] ?? "logs/stuffinabox-.log",
        rollingInterval: RollingInterval.Day,
        // ~1 day of data for now (config-overridable): keep the current day's file only, and
        // roll to a new file if it exceeds the size cap so a single day can't grow unbounded.
        retainedFileCountLimit: context.Configuration.GetValue<int?>("Logging:File:RetainedFileCount") ?? 1,
        fileSizeLimitBytes: context.Configuration.GetValue<long?>("Logging:File:SizeLimitBytes") ?? 50L * 1024 * 1024,
        rollOnFileSizeLimit: true,
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

// Behind App Service (a reverse proxy that terminates TLS) honour X-Forwarded-Proto/For so the
// request scheme is https — HttpsRedirection/HSTS and secure-cookie decisions see the real
// scheme, and the client IP (rate limiting, logs) is the caller, not the proxy. App Service sets
// these headers and strips inbound copies, so clearing KnownNetworks/KnownProxies is safe.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// Startup diagnostic: confirms the environment and whether the OAuth client ids resolved
// at runtime (values themselves are never logged). If these are false in Development, the
// user-secrets store didn't load — the resolved path/user pins down WHY (e.g. the process
// inheriting a different APPDATA and thus looking in another profile's store).
if (app.Environment.IsDevelopment())
{
    var secretsPath = Microsoft.Extensions.Configuration.UserSecrets.PathHelper
        .GetSecretsPathFromSecretsId("2d43a745-3dac-489a-9470-798817153c40");

    // File.Exists hides the reason (returns false on access errors too) — attempt an
    // actual read and surface the concrete exception instead.
    string readProbe;
    try
    {
        var text = File.ReadAllText(secretsPath);
        readProbe = $"OK ({text.Length} chars)";
    }
    catch (Exception e)
    {
        readProbe = $"{e.GetType().Name}: {e.Message}";
    }

    string dirProbe;
    try
    {
        var dir = Path.GetDirectoryName(secretsPath)!;
        dirProbe = Directory.Exists(dir)
            ? $"dir exists, {Directory.GetFiles(dir).Length} file(s)"
            : "dir MISSING";
    }
    catch (Exception e)
    {
        dirProbe = $"{e.GetType().Name}: {e.Message}";
    }

    // Delimited + length so an invisible character (trailing space, stray quote) in the
    // inherited variable shows up; the known-folder value is what Windows itself says.
    var appDataEnv = Environment.GetEnvironmentVariable("APPDATA") ?? "";
    app.Logger.LogInformation(
        "OAuth startup check — env={Env}, Google ClientId set={Google}, Microsoft ClientId set={Microsoft}, " +
        "secretsPath={Path}, read={Read}, dir={Dir}, user={User}, " +
        "APPDATA=|{AppData}| (len={Len}), knownFolder=|{Known}|",
        app.Environment.EnvironmentName,
        !string.IsNullOrWhiteSpace(app.Configuration["OAuth:Google:ClientId"]),
        !string.IsNullOrWhiteSpace(app.Configuration["OAuth:Microsoft:ClientId"]),
        secretsPath, readProbe, dirProbe,
        Environment.UserName,
        appDataEnv, appDataEnv.Length,
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
}

// Prepare the database on startup. SQLite (dev/tests) builds the schema straight from
// the model — no migrations to maintain for the throwaway dev DB. Postgres (production)
// applies the committed migrations. Skipped under EF design-time tooling (migrations
// add/scaffold), which builds this host but must not touch a live database.
if (!EF.IsDesignTime)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsSqlite())
    {
        // SQLite EnsureCreated isn't concurrency-safe. Serialize in-process (parallel
        // integration tests share one host type) and tolerate a concurrent creator in another
        // process (dev: Web + Admin point at the same file). Postgres uses migrations instead.
        lock (DbInitLock)
        {
            try { db.Database.EnsureCreated(); }
            catch (Microsoft.Data.Sqlite.SqliteException) { /* created concurrently */ }
        }
    }
    else
        db.Database.Migrate();

    // Fill the plan catalog on first run (idempotent; admin owns it thereafter).
    await StuffInABox.Infrastructure.Admin.PlanSeeder.SeedAsync(db);
}

// First: correct scheme/client IP from the App Service proxy before anything else runs.
app.UseForwardedHeaders();

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
app.MapSubscriptionEndpoints();
app.MapAccountEndpoints();
app.MapVersionEndpoints();

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

public partial class Program
{
    // Serializes SQLite schema creation across in-process hosts (parallel integration tests).
    private static readonly object DbInitLock = new();
}
