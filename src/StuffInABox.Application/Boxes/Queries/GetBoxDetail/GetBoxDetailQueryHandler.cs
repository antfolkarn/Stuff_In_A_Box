using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Boxes.Queries.GetBoxDetail;

public class GetBoxDetailQueryHandler(
    IBoxRepository boxes,
    ICurrentUserService currentUser) : IRequestHandler<GetBoxDetailQuery, BoxDetailDto?>
{
    public async Task<BoxDetailDto?> Handle(GetBoxDetailQuery request, CancellationToken ct)
    {
        var ownerId = currentUser.UserId;
        var box = await boxes.GetByNumberAsync(new BoxNumber(request.BoxNumber), ownerId, ct);
        if (box is null) return null;
        return new BoxDetailDto(box.Number.Value, box.Label, box.SpaceId);
    }
}
