using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Boxes.Queries.GetBoxDetail;

public class GetBoxDetailQueryHandler(
    IBoxRepository boxes,
    ISpaceAccessService access,
    ICurrentUserService currentUser) : IRequestHandler<GetBoxDetailQuery, BoxDetailDto?>
{
    public async Task<BoxDetailDto?> Handle(GetBoxDetailQuery request, CancellationToken ct)
    {
        var number = new BoxNumber(request.BoxNumber);

        if (request.SpaceId is Guid spaceId)
        {
            var ownerId = await access.RequireSpaceAsync(spaceId, ct: ct);
            var box = await boxes.GetByNumberAsync(number, ownerId, ct);
            // Guard: the box must belong to the space we authorized against, so a member
            // can't reach the owner's boxes in a space they weren't invited to.
            if (box is null || box.SpaceId != spaceId) return null;
            return new BoxDetailDto(box.Number.Value, box.Label, box.SpaceId);
        }

        // No space context (QR deep link): only your own boxes resolve.
        var own = await boxes.GetByNumberAsync(number, currentUser.UserId, ct);
        return own is null ? null : new BoxDetailDto(own.Number.Value, own.Label, own.SpaceId);
    }
}
