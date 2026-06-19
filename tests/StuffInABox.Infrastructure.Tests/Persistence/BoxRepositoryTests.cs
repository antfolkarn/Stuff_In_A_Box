using Microsoft.EntityFrameworkCore;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;
using StuffInABox.Infrastructure.Persistence;
using StuffInABox.Infrastructure.Persistence.Repositories;

namespace StuffInABox.Infrastructure.Tests.Persistence;

public class BoxRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BoxRepository _repo;
    private readonly UserId _userId = new(Guid.NewGuid());
    private readonly Guid _spaceId = Guid.NewGuid();

    public BoxRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _repo = new BoxRepository(_db);
    }

    [Fact]
    public async Task GetNextBoxNumber_WhenNoBoxes_ReturnsOne()
    {
        var next = await _repo.GetNextBoxNumberAsync(_userId);
        Assert.Equal(1, next.Value);
    }

    [Fact]
    public async Task GetNextBoxNumber_AfterAdd_IncrementsCorrectly()
    {
        await _repo.AddAsync(Box.Create(new BoxNumber(1), _spaceId, _userId, "Låda 1"));
        await _repo.AddAsync(Box.Create(new BoxNumber(2), _spaceId, _userId, "Låda 2"));

        var next = await _repo.GetNextBoxNumberAsync(_userId);
        Assert.Equal(3, next.Value);
    }

    [Fact]
    public async Task AddAndGetByNumber_ReturnsBox()
    {
        var box = Box.Create(new BoxNumber(5), _spaceId, _userId, "Verktyg");
        await _repo.AddAsync(box);

        var found = await _repo.GetByNumberAsync(new BoxNumber(5), _userId);
        Assert.NotNull(found);
        Assert.Equal("Verktyg", found!.Label);
    }

    [Fact]
    public async Task MoveBox_UpdatesSpaceId_NumberUnchanged()
    {
        var box = Box.Create(new BoxNumber(1), _spaceId, _userId, "Flytta mig");
        await _repo.AddAsync(box);

        var newSpaceId = Guid.NewGuid();
        box.MoveTo(newSpaceId);
        await _repo.UpdateAsync(box);

        var updated = await _repo.GetByNumberAsync(new BoxNumber(1), _userId);
        Assert.Equal(newSpaceId, updated!.SpaceId);
        Assert.Equal(1, updated.Number.Value);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }
}
