using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StuffInABox.Domain.Entities;
using StuffInABox.Infrastructure.Admin;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Infrastructure.Tests.Admin;

public class PlanCatalogTests : IDisposable
{
    private const string Cs = "Data Source=plancat_test;Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;
    private readonly IServiceScopeFactory _scopeFactory;

    public PlanCatalogTests()
    {
        _keepAlive = new SqliteConnection(Cs);
        _keepAlive.Open();
        using (var db = NewDb()) db.Database.EnsureCreated();
        _scopeFactory = new FakeScopeFactory(NewDb);
    }

    public void Dispose() => _keepAlive.Dispose();

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(Cs).Options);

    private void Seed(params Plan[] plans)
    {
        using var db = NewDb();
        db.Plans.AddRange(plans);
        db.SaveChanges();
    }

    [Fact]
    public void EmptyTable_UsesBuiltInDefaults()
    {
        var catalog = new PlanCatalog(_scopeFactory);

        Assert.Equal(["free", "medium", "large"], catalog.Tiers);
        Assert.Equal(5, catalog.GetPlan("free")!.AiPhotosPerMonth);
        Assert.Equal(-1, catalog.GetPlan("large")!.MaxItems); // unlimited
        Assert.Equal(5, catalog.GetPlan("large")!.MaxMembers); // capped, not unlimited
    }

    [Fact]
    public void ReadsFromDb_InSortOrder()
    {
        Seed(
            Plan.Create("pro", 129, -1, -1, 5, 2000, 20480, true, true, true, 1),
            Plan.Create("starter", 0, 2, 200, 1, 10, 500, false, false, false, 0));

        var catalog = new PlanCatalog(_scopeFactory);

        Assert.Equal(["starter", "pro"], catalog.Tiers); // by SortOrder
        Assert.Equal(129, catalog.GetPlan("pro")!.PriceSek);
        Assert.True(catalog.IsValidTier("STARTER")); // case-insensitive
        Assert.False(catalog.IsValidTier("free"));   // DB is the source of truth, not defaults
    }

    [Fact]
    public void Reload_PicksUpChanges()
    {
        var catalog = new PlanCatalog(_scopeFactory);
        Assert.Equal(["free", "medium", "large"], catalog.Tiers); // defaults (empty table)

        Seed(Plan.Create("solo", 0, 1, 50, 1, 3, 100, false, false, false, 0));
        catalog.Reload();

        Assert.Equal(["solo"], catalog.Tiers);
    }

    // Minimal IServiceScopeFactory that hands out a fresh AppDbContext per scope.
    private sealed class FakeScopeFactory(Func<AppDbContext> factory) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new Scope(factory());

        private sealed class Scope(AppDbContext db) : IServiceScope, IServiceProvider
        {
            public IServiceProvider ServiceProvider => this;
            public object? GetService(Type serviceType) => serviceType == typeof(AppDbContext) ? db : null;
            public void Dispose() => db.Dispose();
        }
    }
}
