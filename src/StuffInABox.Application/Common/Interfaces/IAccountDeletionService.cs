using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Common.Interfaces;

/// <summary>Permanently deletes a user and everything they own. Shared by the consumer
/// self-service "delete my account" (GDPR) and the admin "delete user" operation, so both
/// paths run the exact same cascade.</summary>
public interface IAccountDeletionService
{
    Task DeleteAsync(UserId userId, CancellationToken ct = default);
}
