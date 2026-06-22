using Microsoft.EntityFrameworkCore;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Infrastructure.Persistence.Repositories;

public class PasswordResetTokenRepository(AppDbContext db) : IPasswordResetTokenRepository
{
    public async Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default) =>
        await db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        await db.PasswordResetTokens.AddAsync(token, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        db.PasswordResetTokens.Update(token);
        await db.SaveChangesAsync(ct);
    }

    public async Task InvalidateAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var tokens = await db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .ToListAsync(ct);
        foreach (var t in tokens)
            t.Use(now);
        await db.SaveChangesAsync(ct);
    }

    public Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default) =>
        db.PasswordResetTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync(ct);
}
