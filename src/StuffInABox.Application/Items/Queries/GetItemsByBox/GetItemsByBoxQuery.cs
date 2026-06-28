using MediatR;
using StuffInABox.Domain.Entities;

namespace StuffInABox.Application.Items.Commands.AddItem;

public sealed record GetItemsByBoxQuery(int BoxNumber, Guid SpaceId) : IRequest<IReadOnlyList<ItemDto>>;

public sealed record ItemDto(
    Guid Id, string Name, IReadOnlyList<string> Tags, string? PhotoUrl, ItemEnrichmentStatus Status);
