using MediatR;
using StuffInABox.Application.Admin;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Subscription.Queries;

public sealed class GetSubscriptionQueryHandler(
    ICurrentUserService currentUser,
    IUserSettingsRepository settingsRepo,
    ISpaceRepository spaceRepo,
    IItemRepository itemRepo,
    IPlanCatalog catalog)
    : IRequestHandler<GetSubscriptionQuery, SubscriptionDto>
{
    public async Task<SubscriptionDto> Handle(GetSubscriptionQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var settings = await settingsRepo.GetAsync(userId.Value, ct);
        var tier = settings?.PlanTier ?? UserSettings.DefaultPlanTier;

        // The stored tier might no longer exist in the catalog — fall back to the first plan.
        var current = catalog.GetPlan(tier) ?? catalog.Plans.FirstOrDefault();
        tier = current?.Tier ?? tier;

        var spaces = (await spaceRepo.GetAllAsync(userId, ct)).Count;
        var items = (await itemRepo.GetByOwnerAsync(userId, ct)).Count;

        var usage = new SubscriptionUsageDto(
            Spaces: spaces,
            MaxSpaces: current?.MaxSpaces ?? -1,
            Items: items,
            MaxItems: current?.MaxItems ?? -1);

        var plans = catalog.Plans.Select(p => new PlanOptionDto(
            p.Tier, p.PriceSek, p.MaxSpaces, p.MaxItems, p.MaxMembers,
            p.AiPhotosPerMonth, p.StorageMb, p.ClaudeEnrichment, p.PriorityQueue, p.AllThemes,
            Current: string.Equals(p.Tier, tier, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return new SubscriptionDto(tier, current?.PriceSek ?? 0, usage, plans);
    }
}
