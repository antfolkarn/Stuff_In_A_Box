using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Infrastructure.Tagging;

public class TagEnrichmentWorker(
    EnrichmentQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<TagEnrichmentWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await EnrichAsync(request, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Tag enrichment failed for item {ItemId}", request.ItemId);
            }
        }
    }

    private async Task EnrichAsync(EnrichmentRequest request, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var taggingService = scope.ServiceProvider.GetRequiredService<ITaggingService>();
        var itemRepo = scope.ServiceProvider.GetRequiredService<IItemRepository>();

        var item = await itemRepo.GetByIdAsync(request.ItemId, ct);
        if (item is null) return;

        var tags = await taggingService.GenerateTagsAsync(request.ItemName, ct);
        item.MergeTags(tags);
        await itemRepo.UpdateAsync(item, ct);
    }
}
