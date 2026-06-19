using MediatR;

namespace StuffInABox.Application.Items.Commands.AddItem;

public sealed record GetItemsByBoxQuery(int BoxNumber) : IRequest<IReadOnlyList<ItemDto>>;

public sealed record ItemDto(Guid Id, string Name, IReadOnlyList<string> Tags, string? PhotoUrl);
