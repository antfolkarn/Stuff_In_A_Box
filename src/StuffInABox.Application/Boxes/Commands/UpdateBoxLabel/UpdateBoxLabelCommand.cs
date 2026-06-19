using MediatR;

namespace StuffInABox.Application.Boxes.Commands.UpdateBoxLabel;

public sealed record UpdateBoxLabelCommand(int BoxNumber, string Label) : IRequest;
