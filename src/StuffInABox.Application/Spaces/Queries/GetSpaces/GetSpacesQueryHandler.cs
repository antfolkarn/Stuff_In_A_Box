using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Spaces.Queries.GetSpaces;

public sealed class GetSpacesQueryHandler(
    ISpaceRepository spaceRepo,
    IBoxRepository boxRepo,
    IItemRepository itemRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<GetSpacesQuery, IReadOnlyList<SpaceDto>>
{
    public async Task<IReadOnlyList<SpaceDto>> Handle(GetSpacesQuery request, CancellationToken ct)
    {
        var spaces = await spaceRepo.GetAllAsync(currentUser.UserId, ct);
        var result = new List<SpaceDto>();

        foreach (var space in spaces)
        {
            var boxes = await boxRepo.GetBySpaceAsync(space.Id, currentUser.UserId, ct);
            var itemCount = 0;
            foreach (var box in boxes)
            {
                var items = await itemRepo.GetByBoxAsync(box.Number, currentUser.UserId, ct);
                itemCount += items.Count;
            }
            result.Add(new SpaceDto(space.Id, space.Name, space.Code.Value, space.Icon, boxes.Count, itemCount));
        }

        return result;
    }
}
