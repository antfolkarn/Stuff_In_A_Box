using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Boxes.Queries.GetBoxesBySpace;

public class GetBoxesBySpaceQueryHandler(
    IBoxRepository boxes,
    IItemRepository items,
    ISpaceAccessService access) : IRequestHandler<GetBoxesBySpaceQuery, IReadOnlyList<BoxDto>>
{
    public async Task<IReadOnlyList<BoxDto>> Handle(GetBoxesBySpaceQuery request, CancellationToken ct)
    {
        var ownerId = await access.RequireSpaceAsync(request.SpaceId, ct: ct);
        var boxList = await boxes.GetBySpaceAsync(request.SpaceId, ownerId, ct);

        // One query for all the owner's item counts instead of one per box (avoids N+1).
        var counts = await items.GetCountsByBoxAsync(ownerId, ct);

        return boxList
            .OrderBy(b => b.Number.Value)
            .Select(box => new BoxDto(
                box.Number.Value, box.Label, box.SpaceId, counts.GetValueOrDefault(box.Number.Value)))
            .ToList();
    }
}
