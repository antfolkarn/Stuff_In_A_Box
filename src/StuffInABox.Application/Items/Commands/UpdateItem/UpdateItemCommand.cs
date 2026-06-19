using MediatR;

namespace StuffInABox.Application.Items.Commands.UpdateItem;

/// <summary>
/// Updates an item's name and/or its tags. Null fields are left unchanged.
/// </summary>
public sealed record UpdateItemCommand(Guid ItemId, string? Name, IReadOnlyList<string>? Tags) : IRequest;
