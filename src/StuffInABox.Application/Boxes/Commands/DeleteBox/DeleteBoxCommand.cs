using MediatR;

namespace StuffInABox.Application.Boxes.Commands.DeleteBox;

public sealed record DeleteBoxCommand(int BoxNumber, Guid SpaceId) : IRequest;
