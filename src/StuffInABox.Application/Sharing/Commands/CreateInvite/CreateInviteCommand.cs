using System.Security.Cryptography;
using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Sharing.Commands.CreateInvite;

/// <summary>Owner creates (or returns the existing) share link for a space.</summary>
public sealed record CreateInviteCommand(Guid SpaceId) : IRequest<InviteDto>;

public sealed class CreateInviteCommandHandler(
    ISpaceInviteRepository invites,
    ISpaceAccessService access)
    : IRequestHandler<CreateInviteCommand, InviteDto>
{
    public async Task<InviteDto> Handle(CreateInviteCommand request, CancellationToken ct)
    {
        var ownerId = await access.RequireSpaceAsync(request.SpaceId, ownerOnly: true, ct);

        // Idempotent: reuse the active link if one already exists.
        var existing = await invites.GetActiveBySpaceAsync(request.SpaceId, ct);
        if (existing is not null)
            return new InviteDto(existing.Token);

        var invite = SpaceInvite.Create(request.SpaceId, GenerateToken(), ownerId);
        await invites.AddAsync(invite, ct);
        return new InviteDto(invite.Token);
    }

    // 24 random bytes → URL-safe base64 (no padding), ~32 chars.
    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
