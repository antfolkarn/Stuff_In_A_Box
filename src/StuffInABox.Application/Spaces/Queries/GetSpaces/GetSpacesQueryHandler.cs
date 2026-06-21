using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Spaces.Queries.GetSpaces;

public sealed class GetSpacesQueryHandler(
    IBoxRepository boxRepo,
    IItemRepository itemRepo,
    ISpaceMembershipRepository membershipRepo,
    ISpaceAccessService access,
    ICurrentUserService currentUser)
    : IRequestHandler<GetSpacesQuery, IReadOnlyList<SpaceDto>>
{
    public async Task<IReadOnlyList<SpaceDto>> Handle(GetSpacesQuery request, CancellationToken ct)
    {
        var me = currentUser.UserId;
        var spaces = await access.GetAccessibleSpacesAsync(ct);
        var result = new List<SpaceDto>();

        foreach (var space in spaces)
        {
            // All content is owned by the space owner, so count against OwnerId.
            var boxes = await boxRepo.GetBySpaceAsync(space.Id, space.OwnerId, ct);
            var itemCount = 0;
            foreach (var box in boxes)
            {
                var items = await itemRepo.GetByBoxAsync(box.Number, space.OwnerId, ct);
                itemCount += items.Count;
            }

            var isOwner = space.OwnerId == me;
            var memberCount = isOwner ? (await membershipRepo.GetBySpaceAsync(space.Id, ct)).Count : 0;

            result.Add(new SpaceDto(
                space.Id, space.Name, space.Code.Value, space.Icon,
                boxes.Count, itemCount, isOwner, memberCount));
        }

        return result;
    }
}
