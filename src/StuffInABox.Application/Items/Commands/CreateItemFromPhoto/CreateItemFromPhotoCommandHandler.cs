using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Application.Items.Commands.UploadItemPhoto;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Items.Commands.CreateItemFromPhoto;

public sealed class CreateItemFromPhotoCommandHandler(
    IItemRepository itemRepo,
    IBoxRepository boxRepo,
    IImageProcessor imageProcessor,
    IStorageService storage,
    ISpaceAccessService access,
    IImageRecognitionQueue recognitionQueue)
    : IRequestHandler<CreateItemFromPhotoCommand, CreateItemFromPhotoResult>
{
    public async Task<CreateItemFromPhotoResult> Handle(CreateItemFromPhotoCommand request, CancellationToken ct)
    {
        var ownerId = await access.RequireSpaceAsync(request.SpaceId, ct: ct);
        var boxNumber = new BoxNumber(request.BoxNumber);

        var box = await boxRepo.GetByNumberAsync(boxNumber, ownerId, ct);
        if (box is null || box.SpaceId != request.SpaceId)
            throw new NotFoundException(nameof(Box), request.BoxNumber);

        if (request.Content.Length == 0)
            throw new InvalidImageException("Tom fil.");
        if (request.Content.Length > UploadItemPhotoCommandHandler.MaxBytes)
            throw new InvalidImageException("Bilden är för stor (max 10 MB).");

        // Validates magic bytes + strips EXIF by re-encoding (same as the photo upload path).
        var processed = imageProcessor.ProcessAndStripMetadata(request.Content);

        using var stream = new MemoryStream(processed.Bytes);
        var storageKey = await storage.StoreAsync(
            stream, $"photo{processed.Extension}", processed.ContentType, ct);

        // Content is owned by the space owner regardless of which member added it.
        var item = Item.CreateFromPhoto(boxNumber, ownerId);
        item.SetPhoto(storageKey);
        await itemRepo.AddAsync(item, ct);

        // Fire-and-forget: a worker reloads the photo and fills in name + tags.
        recognitionQueue.EnqueueRecognition(item.Id);

        return new CreateItemFromPhotoResult(
            item.Id, item.Name, storage.GetUrl(storageKey), item.EnrichmentStatus);
    }
}
