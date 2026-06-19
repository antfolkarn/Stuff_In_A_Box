using MediatR;

namespace StuffInABox.Application.Spaces.Commands.CreateSpace;

public sealed record CreateSpaceCommand(string Name, string Icon) : IRequest<CreateSpaceResult>;

public sealed record CreateSpaceResult(Guid SpaceId, string Name, string Code, string Icon);
