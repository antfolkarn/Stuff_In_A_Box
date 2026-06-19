using MediatR;

namespace StuffInABox.Application.Items.Commands.DeleteItem;

public sealed record DeleteItemCommand(Guid ItemId) : IRequest;
