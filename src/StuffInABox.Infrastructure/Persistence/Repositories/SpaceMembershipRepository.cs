using Microsoft.EntityFrameworkCore;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Infrastructure.Persistence.Repositories;

public class SpaceMembershipRepository(AppDbContext db) : ISpaceMembershipRepository
{
    public Task<bool> ExistsAsync(Guid spaceId, UserId userId, CancellationToken ct = default) =>
        db.SpaceMemberships.AnyAsync(m => m.SpaceId == spaceId && m.UserId == userId, ct);

    public async Task<IReadOnlyList<SpaceMembership>> GetByUserAsync(UserId userId, CancellationToken ct = default) =>
        await db.SpaceMemberships.Where(m => m.UserId == userId).ToListAsync(ct);

    public async Task<IReadOnlyList<SpaceMembership>> GetBySpaceAsync(Guid spaceId, CancellationToken ct = default) =>
        await db.SpaceMemberships.Where(m => m.SpaceId == spaceId).ToListAsync(ct);

    public Task<int> CountBySpaceAsync(Guid spaceId, CancellationToken ct = default) =>
        db.SpaceMemberships.CountAsync(m => m.SpaceId == spaceId, ct);

    public async Task AddAsync(SpaceMembership membership, CancellationToken ct = default)
    {
        await db.SpaceMemberships.AddAsync(membership, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Guid spaceId, UserId userId, CancellationToken ct = default)
    {
        var membership = await db.SpaceMemberships
            .FirstOrDefaultAsync(m => m.SpaceId == spaceId && m.UserId == userId, ct);
        if (membership is null) return;
        db.SpaceMemberships.Remove(membership);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAllForSpaceAsync(Guid spaceId, CancellationToken ct = default)
    {
        var rows = await db.SpaceMemberships.Where(m => m.SpaceId == spaceId).ToListAsync(ct);
        if (rows.Count == 0) return;
        db.SpaceMemberships.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
    }

    public Task RemoveAllForUserAsync(UserId userId, CancellationToken ct = default) =>
        db.SpaceMemberships.Where(m => m.UserId == userId).ExecuteDeleteAsync(ct);
}
