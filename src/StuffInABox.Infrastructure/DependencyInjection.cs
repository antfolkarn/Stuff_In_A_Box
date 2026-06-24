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
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();

        // Email: "log" (default — writes the message to the log so flows work without a
        // provider) or a real provider added later behind this flag.
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

        // Image recognition: "none" (default, no-op) or "ollama" (local vision model).
        var recognitionProvider = config["ImageRecognition:Provider"] ?? "none";
        if (recognitionProvider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<IImageRecognitionService, Recognition.OllamaImageRecognitionService>(
                client => client.Timeout = TimeSpan.FromSeconds(120)); // local vision inference can be slow
        else
            services.AddSingleton<IImageRecognitionService, Recognition.NullImageRecognitionService>();

        services.AddSingleton<EnrichmentQueue>();
        services.AddSingleton<IEnrichmentQueue>(sp => sp.GetRequiredService<EnrichmentQueue>());
        services.AddHostedService<TagEnrichmentWorker>();

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
