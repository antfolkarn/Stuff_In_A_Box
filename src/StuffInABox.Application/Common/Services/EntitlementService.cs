using StuffInABox.Application.Admin;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Common.Services;

/// <summary>Resolves the owner's plan from the catalog and enforces its numeric limits.</summary>
public sealed class EntitlementService(
    IPlanCatalog catalog,
    IUserSettingsRepository settingsRepo,
    ISpaceRepository spaceRepo,
    IItemRepository itemRepo,
    ISpaceMembershipRepository membershipRepo)
    : IEntitlementService
{
    public async Task EnsureCanAddSpaceAsync(UserId owner, CancellationToken ct = default)
    {
        var plan = await ResolvePlanAsync(owner, ct);
        if (plan.MaxSpaces < 0) return;
        if (await spaceRepo.CountByOwnerAsync(owner, ct) >= plan.MaxSpaces)
            throw new QuotaExceededException("spaces", plan.MaxSpaces, plan.Tier);
    }

    public async Task EnsureCanAddItemAsync(UserId owner, CancellationToken ct = default)
    {
        var plan = await ResolvePlanAsync(owner, ct);
        if (plan.MaxItems < 0) return;
        if (await itemRepo.CountByOwnerAsync(owner, ct) >= plan.MaxItems)
            throw new QuotaExceededException("items", plan.MaxItems, plan.Tier);
    }

    public async Task EnsureCanAddMemberAsync(Guid spaceId, UserId owner, CancellationToken ct = default)
    {
        var plan = await ResolvePlanAsync(owner, ct);
        if (plan.MaxMembers < 0) return;
        // MaxMembers counts the owner, so current headcount = members + 1. Adding another is
        // only allowed while that headcount is still below the cap.
        var headcount = await membershipRepo.CountBySpaceAsync(spaceId, ct) + 1;
        if (headcount >= plan.MaxMembers)
            throw new QuotaExceededException("members", plan.MaxMembers, plan.Tier);
    }

    private async Task<PlanInfo> ResolvePlanAsync(UserId owner, CancellationToken ct)
    {
        var settings = await settingsRepo.GetAsync(owner.Value, ct);
        var tier = settings?.PlanTier ?? UserSettings.DefaultPlanTier;
        return catalog.GetPlan(tier)
            ?? catalog.Plans.FirstOrDefault()
            ?? throw new InvalidOperationException("Plan-katalogen är tom.");
    }
}
