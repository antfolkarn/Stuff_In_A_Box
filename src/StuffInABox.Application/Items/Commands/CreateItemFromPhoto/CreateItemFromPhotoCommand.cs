using MediatR;
using StuffInABox.Domain.Entities;

namespace StuffInABox.Application.Items.Commands.CreateItemFromPhoto;

/// <summary>
/// Fast path for the bulk add flow: stores an uploaded photo and creates the item
/// immediately with a placeholder name, then queues background recognition to fill in
/// the real name and tags. Returns right away so the client can upload many photos quickly.
/// </summary>
public sealed record CreateItemFromPhotoCommand(int BoxNumber, Guid SpaceId, byte[] Content, string FileName)
    : IRequest<CreateItemFromPhotoResult>;

public sealed record CreateItemFromPhotoResult(
    Guid ItemId, string Name, string? PhotoUrl, ItemEnrichmentStatus Status);
