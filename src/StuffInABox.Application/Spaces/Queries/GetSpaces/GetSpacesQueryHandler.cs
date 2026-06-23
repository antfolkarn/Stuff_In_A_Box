using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

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

        // Item counts per owner are fetched once (one query each) instead of one query
        // per box, then summed in memory — avoids the N+1 that grew with box count.
        var countsByOwner = new Dictionary<UserId, IReadOnlyDictionary<int, int>>();

        foreach (var space in spaces)
        {
            // All content is owned by the space owner, so count against OwnerId.
            var boxes = await boxRepo.GetBySpaceAsync(space.Id, space.OwnerId, ct);

            if (!countsByOwner.TryGetValue(space.OwnerId, out var counts))
            {
                counts = await itemRepo.GetCountsByBoxAsync(space.OwnerId, ct);
                countsByOwner[space.OwnerId] = counts;
            }
            var itemCount = boxes.Sum(b => counts.GetValueOrDefault(b.Number.Value));

            var isOwner = space.OwnerId == me;
            var memberCount = isOwner ? (await membershipRepo.GetBySpaceAsync(space.Id, ct)).Count : 0;

            result.Add(new SpaceDto(
                space.Id, space.Name, space.Code.Value, space.Icon,
                boxes.Count, itemCount, isOwner, memberCount));
        }

        return result;
    }
}
