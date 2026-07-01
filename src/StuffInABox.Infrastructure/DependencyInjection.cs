using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;
using StuffInABox.Infrastructure.Persistence;
using StuffInABox.Infrastructure.Persistence.Repositories;
using StuffInABox.Infrastructure.Storage;
using StuffInABox.Infrastructure.Tagging;

namespace StuffInABox.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var provider = config["Database:Provider"] ?? "sqlite";
            switch (provider.ToLowerInvariant())
            {
                case "postgres":
                case "postgresql":
                    // Production (e.g. Supabase). Migrations live under Persistence/Migrations.
                    options.UseNpgsql(BuildNpgsqlConnectionString(config));
                    break;
                // To add SQL Server: install Microsoft.EntityFrameworkCore.SqlServer and uncomment:
                // case "sqlserver":
                //     options.UseSqlServer(config.GetConnectionString("Default"));
                //     break;
                default:
                    options.UseSqlite(
                        config.GetConnectionString("Default") ?? "Data Source=stuffinabox.db");
                    break;
            }
        });

        services.AddScoped<ISpaceRepository, SpaceRepository>();
        services.AddScoped<ISpaceMembershipRepository, SpaceMembershipRepository>();
        services.AddScoped<ISpaceInviteRepository, SpaceInviteRepository>();
        services.AddScoped<IBoxRepository, BoxRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IUserIdentityRepository, UserIdentityRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();

        // Account deletion cascade, shared by the consumer self-service delete and admin delete.
        services.AddScoped<Application.Common.Interfaces.IAccountDeletionService,
            Application.Common.Services.AccountDeletionService>();

        // Plan quota enforcement (checked against the space owner).
        services.AddScoped<Application.Common.Interfaces.IEntitlementService,
            Application.Common.Services.EntitlementService>();

        // Email: "log" (default — writes the message to the log so flows work without a
        // provider) or "smtp" (any SMTP provider, configured via Email:Smtp:*). Provider-
        // agnostic, so switching email services is a config change, not a code change.
        var emailProvider = config["Email:Provider"] ?? "log";
        if (emailProvider.Equals("smtp", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IEmailService, Email.SmtpEmailService>();
        else
            services.AddScoped<IEmailService, Email.LoggingEmailService>();

        services.AddSingleton<IPhotoUrlSigner, PhotoUrlSigner>();
        // Storage: "local" disk (default, dev) or "r2"/"s3" object storage (production).
        var storageProvider = config["Storage:Provider"] ?? "local";
        if (storageProvider.Equals("r2", StringComparison.OrdinalIgnoreCase)
            || storageProvider.Equals("s3", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IStorageService, R2StorageService>();
        else
            services.AddScoped<IStorageService, LocalFileStorageService>();
        services.AddSingleton<IImageProcessor, Imaging.SkiaImageProcessor>();

        // Tagging provider: "tokenizer" (default, zero-dependency) or "claude" (LLM API).
        var taggingProvider = config["Tagging:Provider"] ?? "tokenizer";
        if (taggingProvider.Equals("claude", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<ITaggingService, ClaudeTaggingService>();
        else
            services.AddScoped<ITaggingService, TokenizerTaggingService>();

        // Image recognition: "none" (default, no-op), "ollama" (vision model — local, or
        // self-hosted and reached over a tunnel) or "staik" (hosted, OpenAI-compatible
        // vision API at api.staik.se). Endpoint, model and auth token are all config-driven.
        var recognitionProvider = config["ImageRecognition:Provider"] ?? "none";
        if (recognitionProvider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
        {
            // Vision inference can be slow (big models, cold start, tunnel latency).
            var timeout = config.GetValue<int?>("ImageRecognition:Ollama:TimeoutSeconds") ?? 180;
            services.AddHttpClient<IImageRecognitionService, Recognition.OllamaImageRecognitionService>(
                client => client.Timeout = TimeSpan.FromSeconds(timeout));
        }
        else if (recognitionProvider.Equals("staik", StringComparison.OrdinalIgnoreCase))
        {
            var timeout = config.GetValue<int?>("ImageRecognition:Staik:TimeoutSeconds") ?? 180;
            services.AddHttpClient<IImageRecognitionService, Recognition.StaikImageRecognitionService>(
                client => client.Timeout = TimeSpan.FromSeconds(timeout));
        }
        else
            services.AddSingleton<IImageRecognitionService, Recognition.NullImageRecognitionService>();

        // Subscription plan catalog — read by both the consumer app (to show the user's
        // plan + limits) and the admin host (to validate tier changes).
        services.AddSingleton<Application.Admin.IPlanCatalog, Admin.PlanCatalog>();

        services.AddSingleton<EnrichmentQueue>();
        services.AddSingleton<IEnrichmentQueue>(sp => sp.GetRequiredService<EnrichmentQueue>());
        services.AddHostedService<TagEnrichmentWorker>();

        // Background photo recognition (name + tags), processed with bounded concurrency.
        services.AddSingleton<ImageRecognitionQueue>();
        services.AddSingleton<IImageRecognitionQueue>(sp => sp.GetRequiredService<ImageRecognitionQueue>());
        services.AddHostedService<ImageRecognitionWorker>();

        return services;
    }

    /// <summary>Registers the admin-only services (subscription catalog + account admin
    /// operations). Called by the separate admin host on top of <see cref="AddInfrastructure"/>,
    /// not by the consumer app.</summary>
    public static IServiceCollection AddAdminCore(this IServiceCollection services)
    {
        // IPlanCatalog is registered in AddInfrastructure (shared with the consumer app).
        services.AddScoped<Application.Admin.IAdminService, Admin.AdminService>();
        services.AddScoped<Application.Admin.IPlanAdminService, Admin.PlanAdminService>();
        return services;
    }

    // When the connection string asks Npgsql to verify the server certificate
    // (SSL Mode=VerifyCA/VerifyFull) but doesn't point at a CA file, fill in the
    // bundled Supabase root CA. Lets the connection string stay path-agnostic across
    // dev and the container — it only needs "SSL Mode=VerifyFull".
    private static string BuildNpgsqlConnectionString(IConfiguration config)
    {
        var csb = new NpgsqlConnectionStringBuilder(config.GetConnectionString("Default"));
        var verifies = csb.SslMode is SslMode.VerifyCA or SslMode.VerifyFull;
        if (verifies && string.IsNullOrEmpty(csb.RootCertificate))
        {
            var caPath = Path.Combine(AppContext.BaseDirectory, "certs", "prod-ca-2021.crt");
            if (File.Exists(caPath))
                csb.RootCertificate = caPath;
        }
        return csb.ConnectionString;
    }
}
