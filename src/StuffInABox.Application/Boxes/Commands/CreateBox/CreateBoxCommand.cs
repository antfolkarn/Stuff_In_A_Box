using MediatR;

namespace StuffInABox.Application.Boxes.Commands.CreateBox;

public sealed record CreateBoxCommand(Guid SpaceId, string Label) : IRequest<CreateBoxResult>;

public sealed record CreateBoxResult(int BoxNumber, Guid SpaceId, string Label);
