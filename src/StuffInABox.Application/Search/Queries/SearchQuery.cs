using MediatR;

namespace StuffInABox.Application.Search.Queries;

public sealed record SearchQuery(string Query) : IRequest<SearchResultDto>;

public sealed record SearchResultDto(
    IReadOnlyList<SpaceSearchResult> Spaces,
    IReadOnlyList<BoxSearchResult> Boxes,
    IReadOnlyList<ItemSearchResult> Items);

public sealed record SpaceSearchResult(Guid Id, string Name, string Icon, int BoxCount);

public sealed record BoxSearchResult(int Number, string Label, string SpaceName, string? MatchReason);

public sealed record ItemSearchResult(Guid Id, string Name, int BoxNumber, string SpaceName, string? MatchedTag);
