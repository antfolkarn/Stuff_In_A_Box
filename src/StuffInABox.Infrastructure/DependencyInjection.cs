using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                // To add PostgreSQL: install Npgsql.EntityFrameworkCore.PostgreSQL and uncomment:
                // case "postgres":
                //     options.UseNpgsql(config.GetConnectionString("Default"));
                //     break;
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
        services.AddScoped<IBoxRepository, BoxRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IUserIdentityRepository, UserIdentityRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IStorageService, LocalFileStorageService>();
        services.AddSingleton<IImageProcessor, Imaging.SkiaImageProcessor>();

        // Tagging provider: "tokenizer" (default, zero-dependency) or "claude" (LLM API).
        var taggingProvider = config["Tagging:Provider"] ?? "tokenizer";
        if (taggingProvider.Equals("claude", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<ITaggingService, ClaudeTaggingService>();
        else
            services.AddScoped<ITaggingService, TokenizerTaggingService>();

        services.AddSingleton<EnrichmentQueue>();
        services.AddSingleton<IEnrichmentQueue>(sp => sp.GetRequiredService<EnrichmentQueue>());
        services.AddHostedService<TagEnrichmentWorker>();

        return services;
    }
}
