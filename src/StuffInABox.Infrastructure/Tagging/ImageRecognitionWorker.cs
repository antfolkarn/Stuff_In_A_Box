using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Infrastructure.Tagging;

/// <summary>
/// Processes queued photo recognitions in the background. Runs at most
/// <see cref="MaxConcurrency"/> at a time so a burst of uploads can't overload the
/// local vision model (Ollama). Each job reloads the stored photo, runs recognition,
/// and fills in the item's name + tags — always marking the item enriched so the UI
/// stops showing the "analyzing" placeholder even when recognition is off or fails.
/// </summary>
public class ImageRecognitionWorker(
    ImageRecognitionQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ImageRecognitionWorker> logger)
    : BackgroundService
{
    private const int MaxConcurrency = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var gate = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);

        await foreach (var itemId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            await gate.WaitAsync(stoppingToken);
            _ = Task.Run(async () =>
            {
                try
                {
                    await RecognizeAsync(itemId, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Image recognition failed for item {ItemId}", itemId);
                }
                finally
                {
                    gate.Release();
                }
            }, stoppingToken);
        }
    }

    private async Task RecognizeAsync(Guid itemId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var recognizer = scope.ServiceProvider.GetRequiredService<IImageRecognitionService>();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var itemRepo = scope.ServiceProvider.GetRequiredService<IItemRepository>();
        var entitlements = scope.ServiceProvider.GetRequiredService<IEntitlementService>();

        var item = await itemRepo.GetByIdAsync(itemId, ct);
        if (item is null) return;

        var bytes = item.PhotoStorageKey is not null
            ? await storage.GetAsync(item.PhotoStorageKey, ct)
            : null;

        var result = bytes is not null ? await recognizer.RecognizeAsync(bytes, ct) : null;
        if (result is not null)
        {
            item.ApplyRecognition(result.Name, result.Tags);
            // Charge the owner only for a run that actually produced a result.
            await entitlements.RecordAiRunAsync(item.OwnerId, ct);
        }
        else
        {
            // Recognition off/failed/empty — no credit spent; offer "run AI" on demand.
            item.MarkAiSkipped();
        }

        await itemRepo.UpdateAsync(item, ct);
    }
}
