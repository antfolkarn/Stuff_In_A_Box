using MediatR;

namespace StuffInABox.Application.Items.Commands.AddItem;

public sealed record AddItemCommand(int BoxNumber, Guid SpaceId, string Name, IReadOnlyList<string>? Tags = null)
    : IRequest<AddItemResult>;

public sealed record AddItemResult(Guid ItemId, string Name, IReadOnlyList<string> Tags);
