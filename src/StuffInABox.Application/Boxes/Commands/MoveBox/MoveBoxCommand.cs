using MediatR;

namespace StuffInABox.Application.Boxes.Commands.MoveBox;

public sealed record MoveBoxCommand(int BoxNumber, Guid TargetSpaceId) : IRequest;
