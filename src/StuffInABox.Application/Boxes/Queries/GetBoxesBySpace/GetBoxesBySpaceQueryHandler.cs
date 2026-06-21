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

        var result = new List<BoxDto>();
        foreach (var box in boxList.OrderBy(b => b.Number.Value))
        {
            var boxItems = await items.GetByBoxAsync(box.Number, ownerId, ct);
            result.Add(new BoxDto(box.Number.Value, box.Label, box.SpaceId, boxItems.Count));
        }
        return result;
    }
}
