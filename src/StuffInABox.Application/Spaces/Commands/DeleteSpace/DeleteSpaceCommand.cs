using MediatR;

namespace StuffInABox.Application.Spaces.Commands.DeleteSpace;

public sealed record DeleteSpaceCommand(Guid SpaceId) : IRequest;
