using Microsoft.EntityFrameworkCore;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;
using StuffInABox.Infrastructure.Persistence;
using StuffInABox.Infrastructure.Persistence.Repositories;

namespace StuffInABox.Infrastructure.Tests.Persistence;

/// <summary>
/// Guards that <see cref="ItemEnrichmentStatus"/> round-trips correctly. The column has a
/// DB default of Completed (to backfill existing rows), so a regression in the sentinel
/// config would silently store photo items as Completed — and the "analyzing" placeholder
/// + polling in the UI would never trigger.
/// </summary>
public class ItemEnrichmentStatusTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ItemRepository _repo;
    private readonly UserId _userId = new(Guid.NewGuid());
    private readonly BoxNumber _box = new(1);

    public ItemEnrichmentStatusTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _repo = new ItemRepository(_db);
    }

    [Fact]
    public async Task PhotoItem_RoundTripsAsPending()
    {
        var item = Item.CreateFromPhoto(_box, _userId);
        await _repo.AddAsync(item);
        _db.ChangeTracker.Clear();

        var loaded = await _repo.GetByIdAsync(item.Id);

        Assert.NotNull(loaded);
        Assert.Equal(ItemEnrichmentStatus.Pending, loaded!.EnrichmentStatus);
    }

    [Fact]
    public async Task ManualItem_RoundTripsAsCompleted()
    {
        var item = Item.Create(_box, _userId, "Hammare");
        await _repo.AddAsync(item);
        _db.ChangeTracker.Clear();

        var loaded = await _repo.GetByIdAsync(item.Id);

        Assert.Equal(ItemEnrichmentStatus.Completed, loaded!.EnrichmentStatus);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }
}
