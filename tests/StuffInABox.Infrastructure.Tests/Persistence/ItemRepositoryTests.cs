using Microsoft.EntityFrameworkCore;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;
using StuffInABox.Infrastructure.Persistence;
using StuffInABox.Infrastructure.Persistence.Repositories;

namespace StuffInABox.Infrastructure.Tests.Persistence;

public class ItemRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ItemRepository _repo;
    private readonly UserId _userId = new(Guid.NewGuid());

    public ItemRepositoryTests()
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
    public async Task GetCountsByBox_GroupsPerBox_InOneQuery()
    {
        await _repo.AddAsync(Item.Create(new BoxNumber(1), _userId, "Mössa"));
        await _repo.AddAsync(Item.Create(new BoxNumber(1), _userId, "Vantar"));
        await _repo.AddAsync(Item.Create(new BoxNumber(2), _userId, "Hammare"));
        // Another owner's item must not leak into the counts.
        await _repo.AddAsync(Item.Create(new BoxNumber(1), new UserId(Guid.NewGuid()), "Främling"));

        var counts = await _repo.GetCountsByBoxAsync(_userId);

        Assert.Equal(2, counts[1]);
        Assert.Equal(1, counts[2]);
        Assert.False(counts.ContainsKey(3));
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }
}
