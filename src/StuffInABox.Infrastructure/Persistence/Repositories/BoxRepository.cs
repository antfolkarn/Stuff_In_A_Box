using Microsoft.EntityFrameworkCore;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Infrastructure.Persistence.Repositories;

public class BoxRepository(AppDbContext db) : IBoxRepository
{
    public async Task<Box?> GetByNumberAsync(BoxNumber number, UserId ownerId, CancellationToken ct = default) =>
        await db.Boxes.FirstOrDefaultAsync(b => b.Number == number && b.OwnerId == ownerId, ct);

    public async Task<IReadOnlyList<Box>> GetBySpaceAsync(Guid spaceId, UserId ownerId, CancellationToken ct = default) =>
        await db.Boxes.Where(b => b.SpaceId == spaceId && b.OwnerId == ownerId).ToListAsync(ct);

    public async Task<BoxNumber> GetNextBoxNumberAsync(UserId ownerId, CancellationToken ct = default)
    {
        // Load numbers into memory — value object conversion prevents SQL translation of .Value
        var boxes = await db.Boxes
            .Where(b => b.OwnerId == ownerId)
            .ToListAsync(ct);
        var max = boxes.Count > 0 ? boxes.Max(b => b.Number.Value) : 0;
        return new BoxNumber(max + 1);
    }

    public async Task AddAsync(Box box, CancellationToken ct = default)
    {
        await db.Boxes.AddAsync(box, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Box box, CancellationToken ct = default)
    {
        db.Boxes.Update(box);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(BoxNumber number, UserId ownerId, CancellationToken ct = default)
    {
        var box = await GetByNumberAsync(number, ownerId, ct);
        if (box is null) return;
        db.Boxes.Remove(box);
        await db.SaveChangesAsync(ct);
    }
}
