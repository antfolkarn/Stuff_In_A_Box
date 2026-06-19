using MediatR;

namespace StuffInABox.Application.Boxes.Queries.GetBoxesBySpace;

public sealed record GetBoxesBySpaceQuery(Guid SpaceId) : IRequest<IReadOnlyList<BoxDto>>;

public sealed record BoxDto(int Number, string Label, Guid SpaceId, int ItemCount);
