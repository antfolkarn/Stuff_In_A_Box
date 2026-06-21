using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Search.Queries;

public sealed class SearchQueryHandler(
    IBoxRepository boxRepo,
    IItemRepository itemRepo,
    ISpaceAccessService access)
    : IRequestHandler<SearchQuery, SearchResultDto>
{
    public async Task<SearchResultDto> Handle(SearchQuery request, CancellationToken ct)
    {
        var q = request.Query.Trim().ToLowerInvariant();
        var spaces = await access.GetAccessibleSpacesAsync(ct);

        // Boxes paired with their owning space (box numbers are per-owner, so we
        // always carry the space to disambiguate and to navigate the UI).
        var allBoxes = new List<(Box box, Space space)>();
        foreach (var space in spaces)
        {
            var boxes = await boxRepo.GetBySpaceAsync(space.Id, space.OwnerId, ct);
            foreach (var box in boxes)
                allBoxes.Add((box, space));
        }

        // Search each distinct owner's items, then map each hit back to its box/space.
        var items = new List<(Item item, Space space)>();
        foreach (var owner in spaces.Select(s => s.OwnerId).Distinct())
        {
            var hits = await itemRepo.SearchAsync(owner, q, ct);
            foreach (var item in hits)
            {
                var match = allBoxes.FirstOrDefault(b =>
                    b.box.Number == item.BoxNumber && b.box.OwnerId == item.OwnerId);
                if (match.space is not null)
                    items.Add((item, match.space));
            }
        }

        var matchedSpaces = spaces
            .Where(s => s.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Select(s => new SpaceSearchResult(
                s.Id, s.Name, s.Icon, allBoxes.Count(b => b.space.Id == s.Id)))
            .ToList();

        var matchedBoxes = allBoxes
            .Where(b => b.box.Label.Contains(q, StringComparison.OrdinalIgnoreCase)
                        || items.Any(i => i.item.BoxNumber == b.box.Number && i.item.OwnerId == b.box.OwnerId))
            .Select(b =>
            {
                var reason = items
                    .Where(i => i.item.BoxNumber == b.box.Number && i.item.OwnerId == b.box.OwnerId
                                && !i.item.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .Select(i => i.item.Name)
                    .FirstOrDefault();
                // Just the item name — the UI adds the localized "Contains …" wording.
                return new BoxSearchResult(b.box.Number.Value, b.space.Id, b.box.Label, b.space.Name, reason);
            })
            .ToList();

        var matchedItems = items.Select(x =>
        {
            var matchedTag = x.item.Tags.FirstOrDefault(t =>
                t.Contains(q, StringComparison.OrdinalIgnoreCase)
                && !x.item.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
            return new ItemSearchResult(
                x.item.Id, x.item.Name, x.item.BoxNumber.Value, x.space.Id, x.space.Name, matchedTag);
        }).ToList();

        return new SearchResultDto(matchedSpaces, matchedBoxes, matchedItems);
    }
}
