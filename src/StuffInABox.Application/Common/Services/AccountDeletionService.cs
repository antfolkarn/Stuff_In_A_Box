using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Common.Services;

/// <summary>Deletes a user and all their data: owned spaces/boxes/items (and photo files),
/// the memberships/invites for those spaces, the user's own memberships in other spaces,
/// sessions, reset and verification tokens, settings, and the identity itself.</summary>
public sealed class AccountDeletionService(
    ISpaceRepository spaceRepo,
    IBoxRepository boxRepo,
    IItemRepository itemRepo,
    ISpaceMembershipRepository membershipRepo,
    ISpaceInviteRepository inviteRepo,
    IRefreshTokenRepository refreshRepo,
    IPasswordResetTokenRepository resetRepo,
    IEmailVerificationTokenRepository verifyRepo,
    IUserSettingsRepository settingsRepo,
    IUserIdentityRepository userRepo,
    IStorageService storage)
    : IAccountDeletionService
{
    public async Task DeleteAsync(UserId userId, CancellationToken ct = default)
    {
        var raw = userId.Value;

        // 1. Photo files for all of the user's items (they own all content in their spaces).
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

        // 5. Sessions, tokens, settings and the account itself.
        await refreshRepo.DeleteAllForUserAsync(raw, ct);
        await resetRepo.DeleteAllForUserAsync(raw, ct);
        await verifyRepo.DeleteAllForUserAsync(raw, ct);
        await settingsRepo.DeleteAsync(raw, ct);
        await userRepo.DeleteAsync(raw, ct);
    }
}
