using MediatR;

namespace StuffInABox.Application.Spaces.Commands.UpdateSpaceIcon;

public sealed record UpdateSpaceIconCommand(Guid SpaceId, string Icon) : IRequest;
