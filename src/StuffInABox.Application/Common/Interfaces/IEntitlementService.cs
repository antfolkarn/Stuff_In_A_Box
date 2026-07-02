using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Common.Interfaces;

/// <summary>Enforces plan quotas. Everything is checked against the <b>space owner</b> (all
/// content is owned by <c>Space.OwnerId</c>), so a member's action counts against the owner's
/// plan, not their own. Each method throws <see cref="Domain.Exceptions.QuotaExceededException"/>
/// when the action would exceed the limit; an unlimited (<c>-1</c>) limit is always allowed.
/// Checks are additive-only, so a downgrade never blocks existing data (grandfathering).</summary>
public interface IEntitlementService
{
    Task EnsureCanAddSpaceAsync(UserId owner, CancellationToken ct = default);
    Task EnsureCanAddItemAsync(UserId owner, CancellationToken ct = default);
    Task EnsureCanAddMemberAsync(Guid spaceId, UserId owner, CancellationToken ct = default);

    /// <summary>Throws if adding <paramref name="addingBytes"/> would exceed the owner's storage limit.</summary>
    Task EnsureCanStoreAsync(UserId owner, long addingBytes, CancellationToken ct = default);

    /// <summary>True if the owner has AI runs left this month (unlimited → always true). A pure
    /// check — nothing is consumed. Gates whether to queue recognition; the credit is only spent
    /// when the run actually produces a result (<see cref="RecordAiRunAsync"/>).</summary>
    Task<bool> HasAiCreditAsync(UserId owner, CancellationToken ct = default);

    /// <summary>Throws <see cref="Domain.Exceptions.QuotaExceededException"/> when the AI quota is
    /// exhausted. Used by the explicit "run AI" action, where being over quota is an error.</summary>
    Task EnsureAiCreditAsync(UserId owner, CancellationToken ct = default);

    /// <summary>Records one AI recognition run that actually happened — called by the worker only
    /// when recognition produced a result, so the quota reflects real usage. No limit check.</summary>
    Task RecordAiRunAsync(UserId owner, CancellationToken ct = default);

    /// <summary>True when the owner's plan grants priority AI processing — their recognition jobs
    /// are queued ahead of non-priority plans.</summary>
    Task<bool> HasPriorityQueueAsync(UserId owner, CancellationToken ct = default);

    /// <summary>True when the owner's plan unlocks every design theme (the <c>AllThemes</c> flag);
    /// otherwise only <see cref="Settings.SettingsOptions.FreeDesigns"/> may be selected.</summary>
    Task<bool> HasAllThemesAsync(UserId owner, CancellationToken ct = default);
}
