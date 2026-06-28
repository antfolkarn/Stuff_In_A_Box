using Microsoft.EntityFrameworkCore;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Infrastructure.Persistence.Repositories;

public class EmailVerificationTokenRepository(AppDbContext db) : IEmailVerificationTokenRepository
{
    public async Task<EmailVerificationToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default) =>
        await db.EmailVerificationTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(EmailVerificationToken token, CancellationToken ct = default)
    {
        await db.EmailVerificationTokens.AddAsync(token, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(EmailVerificationToken token, CancellationToken ct = default)
    {
        db.EmailVerificationTokens.Update(token);
        await db.SaveChangesAsync(ct);
    }

    public async Task InvalidateAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var tokens = await db.EmailVerificationTokens
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .ToListAsync(ct);
        foreach (var t in tokens)
            t.Use(now);
        await db.SaveChangesAsync(ct);
    }
}
