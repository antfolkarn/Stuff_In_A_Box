using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Search.Queries;

public sealed class SearchQueryHandler(
    ISpaceRepository spaceRepo,
    IBoxRepository boxRepo,
    IItemRepository itemRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<SearchQuery, SearchResultDto>
{
    public async Task<SearchResultDto> Handle(SearchQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var q = request.Query.Trim().ToLowerInvariant();

        var spaces = await spaceRepo.GetAllAsync(userId, ct);
        var matchedSpaces = spaces
            .Where(s => s.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var allBoxes = new List<(Domain.Entities.Box box, string spaceName)>();
        foreach (var space in spaces)
        {
            var boxes = await boxRepo.GetBySpaceAsync(space.Id, userId, ct);
            foreach (var box in boxes)
                allBoxes.Add((box, space.Name));
        }

        var items = await itemRepo.SearchAsync(userId, q, ct);

        var matchedBoxes = allBoxes
            .Where(b => b.box.Label.Contains(q, StringComparison.OrdinalIgnoreCase)
                        || items.Any(i => i.BoxNumber == b.box.Number))
            .Select(b =>
            {
                var reason = items
                    .Where(i => i.BoxNumber == b.box.Number && !i.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .Select(i => i.Name)
                    .FirstOrDefault();
                return new BoxSearchResult(b.box.Number.Value, b.box.Label, b.spaceName,
                    reason is not null ? $"Innehåller {reason}" : null);
            })
            .ToList();

        var matchedItems = items.Select(i =>
        {
            var spaceName = allBoxes.FirstOrDefault(b => b.box.Number == i.BoxNumber).spaceName ?? "";
            var matchedTag = i.Tags.FirstOrDefault(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)
                                                         && !i.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
            return new ItemSearchResult(i.Id, i.Name, i.BoxNumber.Value, spaceName, matchedTag);
        }).ToList();

        var spaceResults = matchedSpaces.Select(async s =>
        {
            var boxes = await boxRepo.GetBySpaceAsync(s.Id, userId, ct);
            return new SpaceSearchResult(s.Id, s.Name, s.Icon, boxes.Count);
        });

        return new SearchResultDto(
            await Task.WhenAll(spaceResults),
            matchedBoxes,
            matchedItems);
    }
}
