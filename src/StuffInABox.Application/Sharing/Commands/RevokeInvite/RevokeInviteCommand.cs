using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Sharing.Commands.RevokeInvite;

/// <summary>Owner disables the active share link (existing members keep access).</summary>
public sealed record RevokeInviteCommand(Guid SpaceId) : IRequest;

public sealed class RevokeInviteCommandHandler(
    ISpaceInviteRepository invites,
    ISpaceAccessService access)
    : IRequestHandler<RevokeInviteCommand>
{
    public async Task Handle(RevokeInviteCommand request, CancellationToken ct)
    {
        await access.RequireSpaceAsync(request.SpaceId, ownerOnly: true, ct);

        var existing = await invites.GetActiveBySpaceAsync(request.SpaceId, ct);
        if (existing is null) return;

        existing.Revoke();
        await invites.UpdateAsync(existing, ct);
    }
}
