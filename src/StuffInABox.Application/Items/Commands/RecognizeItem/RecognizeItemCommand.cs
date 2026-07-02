using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Items.Commands.RecognizeItem;

/// <summary>Runs AI recognition on an existing photo item on demand — for items created without AI
/// (monthly quota was spent) or to re-run. Consumes an AI credit; over quota → 403.</summary>
public sealed record RecognizeItemCommand(Guid ItemId) : IRequest;

public sealed class RecognizeItemCommandHandler(
    IItemRepository itemRepo,
    IBoxRepository boxRepo,
    ISpaceAccessService access,
    IEntitlementService entitlements,
    IImageRecognitionQueue recognitionQueue)
    : IRequestHandler<RecognizeItemCommand>
{
    public async Task Handle(RecognizeItemCommand request, CancellationToken ct)
    {
        var item = await itemRepo.GetByIdAsync(request.ItemId, ct)
            ?? throw new NotFoundException(nameof(Item), request.ItemId);

        var box = await boxRepo.GetByNumberAsync(item.BoxNumber, item.OwnerId, ct)
            ?? throw new NotFoundException(nameof(Item), request.ItemId);
        var ownerId = await access.RequireSpaceAsync(box.SpaceId, ct: ct);

        // Only photo items have something for the vision model to look at.
        if (item.PhotoStorageKey is null)
            throw new NotFoundException("Photo", request.ItemId);

        // Already analyzed → nothing to do. Don't let the user spend a credit re-running it.
        if (item.EnrichmentStatus == ItemEnrichmentStatus.Completed)
            return;

        // Over monthly quota → QuotaExceededException → 403. The credit itself is spent by the
        // worker only if recognition produces a result.
        await entitlements.EnsureAiCreditAsync(ownerId, ct);

        item.MarkPendingRecognition();
        await itemRepo.UpdateAsync(item, ct);

        var priority = await entitlements.HasPriorityQueueAsync(ownerId, ct);
        recognitionQueue.EnqueueRecognition(item.Id, priority);
    }
}
