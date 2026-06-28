using StuffInABox.Domain.Entities;

namespace StuffInABox.Domain.Repositories;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(EmailVerificationToken token, CancellationToken ct = default);
    Task UpdateAsync(EmailVerificationToken token, CancellationToken ct = default);
    /// <summary>Invalidate any outstanding verification tokens for a user (e.g. before issuing a new one).</summary>
    Task InvalidateAllForUserAsync(Guid userId, CancellationToken ct = default);
}
