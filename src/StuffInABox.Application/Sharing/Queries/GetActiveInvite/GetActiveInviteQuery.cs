using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Sharing.Queries.GetActiveInvite;

/// <summary>Owner fetches the current share link, if any.</summary>
public sealed record GetActiveInviteQuery(Guid SpaceId) : IRequest<InviteDto?>;

public sealed class GetActiveInviteQueryHandler(
    ISpaceInviteRepository invites,
    ISpaceAccessService access)
    : IRequestHandler<GetActiveInviteQuery, InviteDto?>
{
    public async Task<InviteDto?> Handle(GetActiveInviteQuery request, CancellationToken ct)
    {
        await access.RequireSpaceAsync(request.SpaceId, ownerOnly: true, ct);
        var existing = await invites.GetActiveBySpaceAsync(request.SpaceId, ct);
        return existing is null ? null : new InviteDto(existing.Token);
    }
}
