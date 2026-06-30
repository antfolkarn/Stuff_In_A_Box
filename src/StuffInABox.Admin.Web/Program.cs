using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using StuffInABox.Admin.Web.Endpoints;
using StuffInABox.Infrastructure;
using StuffInABox.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Dev convenience: when on SQLite with no explicit connection, point at the consumer app's
// database file resolved relative to THIS host's content root (deterministic regardless of
// the working directory), so the admin app sees the same users as StuffInABox.Web when both
// run from Visual Studio. Production (Postgres) is unaffected — it uses the configured string.
var dbProvider = builder.Configuration["Database:Provider"] ?? "sqlite";
if (dbProvider.Equals("sqlite", StringComparison.OrdinalIgnoreCase)
    && string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Default")))
{
    var sharedDb = Path.GetFullPath(
        Path.Combine(builder.Environment.ContentRootPath, "..", "StuffInABox.Web", "stuffinabox.db"));
    builder.Configuration["ConnectionStrings:Default"] = $"Data Source={sharedDb}";
}

// Shared core: EF Core + repositories (same AppDbContext as the consumer app), plus the
// admin-only services (plan catalog + account admin operations).
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAdminCore();
builder.Services.AddHttpContextAccessor();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Sign-in via Microsoft Entra ID (single tenant). Authorization is simply "authenticated":
// because the authority is locked to one tenant, only that tenant's users can sign in, and
// every signed-in user is an admin. Pure authentication (ID token only) → no client secret.
var instance = (builder.Configuration["AzureAd:Instance"] ?? "https://login.microsoftonline.com/").TrimEnd('/');
var tenantId = builder.Configuration["AzureAd:TenantId"]
    ?? throw new InvalidOperationException("AzureAd:TenantId must be configured.");
var clientId = builder.Configuration["AzureAd:ClientId"]
    ?? throw new InvalidOperationException("AzureAd:ClientId must be configured.");

builder.Services.AddAuthentication(options =>
{
    // Cookie is the default for both sign-in and challenge, so an unauthenticated request
    // fails fast against the cookie scheme (→ 401 for /api, see below) without ever touching
    // Entra. The Entra redirect happens only when /login challenges OIDC explicitly.
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "sib_admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Events.OnRedirectToLogin = ctx =>
    {
        // XHR to the API gets a plain 401 (the SPA shows a sign-in button); a browser
        // navigation to a protected page is sent to /login to start the Entra sign-in.
        if (ctx.Request.Path.StartsWithSegments("/api"))
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else
            ctx.Response.Redirect("/login?returnUrl=" + Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString));
        return Task.CompletedTask;
    };
})
.AddOpenIdConnect(options =>
{
    options.Authority = $"{instance}/{tenantId}/v2.0";
    options.ClientId = clientId;
    // ID-token-only flow: we only need to prove tenant membership, not call any API.
    options.ResponseType = OpenIdConnectResponseType.IdToken;
    options.ResponseMode = "form_post";
    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.GetClaimsFromUserInfoEndpoint = false;
    options.TokenValidationParameters.NameClaimType = "name";
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("admin", policy => policy.RequireAuthenticatedUser());

var app = builder.Build();

// Prepare the dev database. SQLite (dev) builds the schema from the model; Postgres (prod)
// is owned by the consumer app's migrations, so we don't touch it. Skipped at design time.
if (!EF.IsDesignTime)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsSqlite())
        db.Database.EnsureCreated();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapAdminEndpoints();

// Serve the admin console (single-page) for any non-API route.
app.MapFallbackToFile("index.html");

app.Run();
