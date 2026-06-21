using Microsoft.EntityFrameworkCore;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Infrastructure.Persistence.Repositories;

public class SpaceRepository(AppDbContext db) : ISpaceRepository
{
    public async Task<Space?> GetByIdAsync(Guid id, UserId ownerId, CancellationToken ct = default) =>
        await db.Spaces.FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId, ct);

    public async Task<Space?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Spaces.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Space>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
        ids.Count == 0
            ? []
            : await db.Spaces.Where(s => ids.Contains(s.Id)).ToListAsync(ct);

    public async Task<IReadOnlyList<Space>> GetAllAsync(UserId ownerId, CancellationToken ct = default) =>
        await db.Spaces.Where(s => s.OwnerId == ownerId).ToListAsync(ct);

    public async Task AddAsync(Space space, CancellationToken ct = default)
    {
        await db.Spaces.AddAsync(space, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Space space, CancellationToken ct = default)
    {
        db.Spaces.Update(space);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, UserId ownerId, CancellationToken ct = default)
    {
        var space = await GetByIdAsync(id, ownerId, ct);
        if (space is null) return;
        db.Spaces.Remove(space);
        await db.SaveChangesAsync(ct);
    }
}
