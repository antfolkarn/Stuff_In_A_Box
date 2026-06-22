using StuffInABox.Domain.Entities;

namespace StuffInABox.Domain.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(PasswordResetToken token, CancellationToken ct = default);
    Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default);
    /// <summary>Invalidate any outstanding reset tokens for a user (e.g. after a successful reset).</summary>
    Task InvalidateAllForUserAsync(Guid userId, CancellationToken ct = default);
    Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default);
}
