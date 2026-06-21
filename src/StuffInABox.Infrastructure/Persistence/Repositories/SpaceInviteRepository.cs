using Microsoft.EntityFrameworkCore;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Infrastructure.Persistence.Repositories;

public class SpaceInviteRepository(AppDbContext db) : ISpaceInviteRepository
{
    public Task<SpaceInvite?> GetActiveBySpaceAsync(Guid spaceId, CancellationToken ct = default) =>
        db.SpaceInvites.FirstOrDefaultAsync(i => i.SpaceId == spaceId && i.RevokedAt == null, ct);

    public Task<SpaceInvite?> GetActiveByTokenAsync(string token, CancellationToken ct = default) =>
        db.SpaceInvites.FirstOrDefaultAsync(i => i.Token == token && i.RevokedAt == null, ct);

    public async Task AddAsync(SpaceInvite invite, CancellationToken ct = default)
    {
        await db.SpaceInvites.AddAsync(invite, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(SpaceInvite invite, CancellationToken ct = default)
    {
        db.SpaceInvites.Update(invite);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAllForSpaceAsync(Guid spaceId, CancellationToken ct = default)
    {
        var rows = await db.SpaceInvites.Where(i => i.SpaceId == spaceId).ToListAsync(ct);
        if (rows.Count == 0) return;
        db.SpaceInvites.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
    }
}
