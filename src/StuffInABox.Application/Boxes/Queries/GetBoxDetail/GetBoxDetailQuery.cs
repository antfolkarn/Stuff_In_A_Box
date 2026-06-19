using MediatR;

namespace StuffInABox.Application.Boxes.Queries.GetBoxDetail;

public sealed record GetBoxDetailQuery(int BoxNumber) : IRequest<BoxDetailDto?>;

public sealed record BoxDetailDto(int Number, string Label, Guid SpaceId);
