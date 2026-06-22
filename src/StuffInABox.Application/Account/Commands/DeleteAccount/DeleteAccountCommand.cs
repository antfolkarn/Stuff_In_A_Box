using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Account.Commands.DeleteAccount;

/// <summary>
/// GDPR right to erasure: permanently deletes the current user and all their data —
/// owned spaces/boxes/items (and photos), the memberships/invites for those spaces,
/// the user's own memberships in other spaces, sessions, reset tokens, settings and
/// the identity itself.
/// </summary>
public sealed record DeleteAccountCommand : IRequest;

public sealed class DeleteAccountCommandHandler(
    ICurrentUserService currentUser,
    ISpaceRepository spaceRepo,
    IBoxRepository boxRepo,
    IItemRepository itemRepo,
    ISpaceMembershipRepository membershipRepo,
    ISpaceInviteRepository inviteRepo,
    IRefreshTokenRepository refreshRepo,
    IPasswordResetTokenRepository resetRepo,
    IUserSettingsRepository settingsRepo,
    IUserIdentityRepository userRepo,
    IStorageService storage)
    : IRequestHandler<DeleteAccountCommand>
{
    public async Task Handle(DeleteAccountCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var raw = userId.Value;

        // 1. Delete the photo files for all of the user's items (they own all content
        //    in their spaces).
        var items = await itemRepo.GetByOwnerAsync(userId, ct);
        foreach (var item in items)
            if (item.PhotoStorageKey is not null)
                await storage.DeleteAsync(item.PhotoStorageKey, ct);

        // 2. Access grants for the user's owned spaces (memberships + invite links).
        var ownedSpaces = await spaceRepo.GetAllAsync(userId, ct);
        foreach (var space in ownedSpaces)
        {
            await inviteRepo.RemoveAllForSpaceAsync(space.Id, ct);
            await membershipRepo.RemoveAllForSpaceAsync(space.Id, ct);
        }

        // 3. The user's own memberships in other people's shared spaces.
        await membershipRepo.RemoveAllForUserAsync(userId, ct);

        // 4. The user's content + spaces.
        await itemRepo.DeleteAllForOwnerAsync(userId, ct);
        await boxRepo.DeleteAllForOwnerAsync(userId, ct);
        await spaceRepo.DeleteAllForOwnerAsync(userId, ct);

        // 5. Sessions, reset tokens, settings and the account itself.
        await refreshRepo.DeleteAllForUserAsync(raw, ct);
        await resetRepo.DeleteAllForUserAsync(raw, ct);
        await settingsRepo.DeleteAsync(raw, ct);
        await userRepo.DeleteAsync(raw, ct);
    }
}
