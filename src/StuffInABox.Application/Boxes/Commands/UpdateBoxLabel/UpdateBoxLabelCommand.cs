using MediatR;

namespace StuffInABox.Application.Boxes.Commands.UpdateBoxLabel;

public sealed record UpdateBoxLabelCommand(int BoxNumber, Guid SpaceId, string Label) : IRequest;
