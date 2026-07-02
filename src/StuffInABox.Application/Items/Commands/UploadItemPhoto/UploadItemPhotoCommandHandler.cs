using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Items.Commands.UploadItemPhoto;

public sealed class UploadItemPhotoCommandHandler(
    IItemRepository itemRepo,
    IBoxRepository boxRepo,
    IImageProcessor imageProcessor,
    IStorageService storage,
    ISpaceAccessService access,
    IEntitlementService entitlements)
    : IRequestHandler<UploadItemPhotoCommand, UploadItemPhotoResult>
{
    // 10 MB cap, matching the plan's upload limit
    public const int MaxBytes = 10 * 1024 * 1024;

    public async Task<UploadItemPhotoResult> Handle(UploadItemPhotoCommand request, CancellationToken ct)
    {
        var item = await itemRepo.GetByIdAsync(request.ItemId, ct)
            ?? throw new NotFoundException(nameof(Item), request.ItemId);

        var box = await boxRepo.GetByNumberAsync(item.BoxNumber, item.OwnerId, ct)
            ?? throw new NotFoundException(nameof(Item), request.ItemId);
        await access.RequireSpaceAsync(box.SpaceId, ct: ct);

        if (request.Content.Length == 0)
            throw new InvalidImageException("Tom fil.");
        if (request.Content.Length > MaxBytes)
            throw new InvalidImageException("Bilden är för stor (max 10 MB).");

        // Validates magic bytes + strips EXIF by re-encoding
        var processed = imageProcessor.ProcessAndStripMetadata(request.Content);

        // Only the delta counts — this replaces the item's existing photo.
        await entitlements.EnsureCanStoreAsync(item.OwnerId, processed.Bytes.Length - item.PhotoSizeBytes, ct);

        using var stream = new MemoryStream(processed.Bytes);
        var storageKey = await storage.StoreAsync(
            stream, $"photo{processed.Extension}", processed.ContentType, ct);

        // Replace any previous photo
        var oldKey = item.PhotoStorageKey;
        item.SetPhoto(storageKey, processed.Bytes.Length);
        await itemRepo.UpdateAsync(item, ct);

        if (oldKey is not null)
            await storage.DeleteAsync(oldKey, ct);

        return new UploadItemPhotoResult(storage.GetUrl(storageKey));
    }
}
