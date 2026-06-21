using MediatR;

namespace StuffInABox.Application.Spaces.Queries.GetSpaces;

public sealed record GetSpacesQuery : IRequest<IReadOnlyList<SpaceDto>>;

public sealed record SpaceDto(
    Guid Id,
    string Name,
    string Code,
    string Icon,
    int BoxCount,
    int ItemCount,
    bool IsOwner,
    int MemberCount);
