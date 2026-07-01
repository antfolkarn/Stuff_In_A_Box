using Microsoft.EntityFrameworkCore;
using StuffInABox.Application.Admin;
using StuffInABox.Domain.Entities;
using StuffInABox.Infrastructure.Admin;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Infrastructure.Tests.Admin;

public class PlanAdminServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly StubCatalog _catalog = new();
    private readonly PlanAdminService _svc;

    public PlanAdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options;
        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _svc = new PlanAdminService(_db, _catalog);
    }

    public void Dispose() => _db.Dispose();

    private static PlanInput Input(string tier, int price = 0, int members = 1) =>
        new(tier, price, 1, 100, members, 5, 250, false, false, false, 0);

    [Fact]
    public async Task Upsert_CreatesThenUpdatesInPlace()
    {
        await _svc.UpsertAsync(Input("gold", price: 10));
        Assert.Equal(10, (await _db.Plans.SingleAsync()).PriceSek);

        await _svc.UpsertAsync(Input("gold", price: 20, members: 3));
        var plan = await _db.Plans.SingleAsync(); // still one row
        Assert.Equal(20, plan.PriceSek);
        Assert.Equal(3, plan.MaxMembers);
        Assert.True(_catalog.Reloads >= 2); // cache refreshed after each write
    }

    [Fact]
    public async Task Delete_RemovesWhenNotInUse()
    {
        await _svc.UpsertAsync(Input("temp"));
        Assert.True(await _svc.DeleteAsync("temp"));
        Assert.False(await _db.Plans.AnyAsync());
    }

    [Fact]
    public async Task Delete_MissingTier_ReturnsFalse()
    {
        Assert.False(await _svc.DeleteAsync("nope"));
    }

    [Fact]
    public async Task Delete_TierInUse_Throws()
    {
        await _svc.UpsertAsync(Input("free"));
        var settings = UserSettings.CreateDefault(Guid.NewGuid());
        settings.SetPlanTier("free");
        _db.UserSettings.Add(settings);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.DeleteAsync("free"));
        Assert.True(await _db.Plans.AnyAsync()); // not removed
    }

    private sealed class StubCatalog : IPlanCatalog
    {
        public int Reloads;
        public void Reload() => Reloads++;
        public IReadOnlyList<string> Tiers => [];
        public IReadOnlyList<PlanInfo> Plans => [];
        public bool IsValidTier(string tier) => true;
        public PlanInfo? GetPlan(string tier) => null;
    }
}
