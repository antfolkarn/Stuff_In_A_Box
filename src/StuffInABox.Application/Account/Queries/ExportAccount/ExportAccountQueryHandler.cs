using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Account.Queries.ExportAccount;

public sealed class ExportAccountQueryHandler(
    ICurrentUserService currentUser,
    IUserIdentityRepository userRepo,
    IUserSettingsRepository settingsRepo,
    ISpaceRepository spaceRepo,
    IBoxRepository boxRepo,
    IItemRepository itemRepo,
    ISpaceMembershipRepository membershipRepo)
    : IRequestHandler<ExportAccountQuery, AccountExport>
{
    public async Task<AccountExport> Handle(ExportAccountQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var raw = userId.Value;

        var identity = await userRepo.FindByIdAsync(raw, ct);
        var account = new AccountInfo(
            raw, identity?.Provider ?? "unknown", identity?.Email, identity?.CreatedAt ?? default);

        var settingsEntity = await settingsRepo.GetAsync(raw, ct);
        var settings = settingsEntity is null ? null : new SettingsExport(settingsEntity.Theme, settingsEntity.Design);

        // Owned spaces with their boxes and items.
        var ownedSpaces = await spaceRepo.GetAllAsync(userId, ct);
        var spaces = new List<SpaceExport>();
        foreach (var space in ownedSpaces)
        {
            var boxes = await boxRepo.GetBySpaceAsync(space.Id, userId, ct);
            var boxExports = new List<BoxExport>();
            foreach (var box in boxes.OrderBy(b => b.Number.Value))
            {
                var items = await itemRepo.GetByBoxAsync(box.Number, userId, ct);
                boxExports.Add(new BoxExport(
                    box.Number.Value, box.Label,
                    items.Select(i => new ItemExport(i.Name, i.Tags, i.PhotoStorageKey is not null)).ToList()));
            }
            spaces.Add(new SpaceExport(space.Name, space.Code.Value, space.Icon, boxExports));
        }

        // Spaces shared with the user (joined as a member).
        var myMemberships = await membershipRepo.GetByUserAsync(userId, ct);
        var ownedIds = ownedSpaces.Select(s => s.Id).ToHashSet();
        var memberSpaceIds = myMemberships.Select(m => m.SpaceId).Where(id => !ownedIds.Contains(id)).ToList();
        var memberSpaces = (await spaceRepo.GetByIdsAsync(memberSpaceIds, ct)).ToDictionary(s => s.Id);

        var sharedWithMe = myMemberships
            .Where(m => memberSpaces.ContainsKey(m.SpaceId))
            .Select(m => new MembershipExport(m.SpaceId, memberSpaces[m.SpaceId].Name, m.CreatedAt))
            .ToList();

        return new AccountExport(account, settings, spaces, sharedWithMe, DateTimeOffset.UtcNow);
    }
}
