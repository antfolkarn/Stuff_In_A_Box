using Microsoft.EntityFrameworkCore;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Infrastructure.Persistence.Repositories;

public class ItemRepository(AppDbContext db) : IItemRepository
{
    public async Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Items.FindAsync([id], ct);

    public async Task<IReadOnlyList<Item>> GetByBoxAsync(BoxNumber boxNumber, UserId ownerId, CancellationToken ct = default) =>
        await db.Items.Where(i => i.BoxNumber == boxNumber && i.OwnerId == ownerId).ToListAsync(ct);

    public async Task<IReadOnlyList<Item>> GetByOwnerAsync(UserId ownerId, CancellationToken ct = default) =>
        await db.Items.Where(i => i.OwnerId == ownerId).ToListAsync(ct);

    public async Task<IReadOnlyList<Item>> SearchAsync(UserId ownerId, string query, CancellationToken ct = default)
    {
        var q = query.ToLowerInvariant();
        // Load all items for the user and filter in memory (tags are JSON-serialized)
        var items = await db.Items.Where(i => i.OwnerId == ownerId).ToListAsync(ct);
        return items
            .Where(i => i.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || i.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public async Task AddAsync(Item item, CancellationToken ct = default)
    {
        await db.Items.AddAsync(item, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Item item, CancellationToken ct = default)
    {
        db.Items.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid itemId, CancellationToken ct = default)
    {
        var item = await GetByIdAsync(itemId, ct);
        if (item is null) return;
        db.Items.Remove(item);
        await db.SaveChangesAsync(ct);
    }

    public Task DeleteAllForOwnerAsync(UserId ownerId, CancellationToken ct = default) =>
        db.Items.Where(i => i.OwnerId == ownerId).ExecuteDeleteAsync(ct);
}
