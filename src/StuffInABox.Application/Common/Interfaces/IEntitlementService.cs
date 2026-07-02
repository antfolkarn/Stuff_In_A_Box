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

    /// <summary>Consumes one AI recognition run against the owner's monthly quota, throwing
    /// <see cref="Domain.Exceptions.QuotaExceededException"/> when it's exhausted. Used by the
    /// explicit "run AI" action, where being over quota should surface as an error.</summary>
    Task EnsureAiCreditAsync(UserId owner, CancellationToken ct = default);

    /// <summary>Like <see cref="EnsureAiCreditAsync"/> but returns false instead of throwing when
    /// the quota is exhausted, so the caller can create the item without AI. Unlimited → true.</summary>
    Task<bool> TryConsumeAiAsync(UserId owner, CancellationToken ct = default);
}
