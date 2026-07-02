using Microsoft.EntityFrameworkCore;
using StuffInABox.Application.Admin;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;
using StuffInABox.Infrastructure.Admin;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Infrastructure.Tests.Admin;

public class AdminServiceListTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly AdminService _svc;

    public AdminServiceListTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options;
        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _svc = new AdminService(_db, new StubCatalog(), new NoopDeletion());
    }

    public void Dispose() => _db.Dispose();

    private async Task<UserIdentity> SeedLinkedPersonAsync(string email = "andree@example.com")
    {
        var primary = UserIdentity.CreateEmail("hashed", "pw", email);
        primary.MarkEmailVerified();
        _db.UserIdentities.Add(primary);
        _db.UserIdentities.Add(UserIdentity.CreateOAuthLinked("google", "sub-1", email, primary.UserId));
        await _db.SaveChangesAsync();
        return primary;
    }

    [Fact]
    public async Task LinkedLogins_ShowAsOneRowWithBothProviders()
    {
        var primary = await SeedLinkedPersonAsync();

        var rows = await _svc.ListUsersAsync(null);

        var row = Assert.Single(rows);
        Assert.Equal(primary.UserId, row.UserId);
        Assert.Equal(["email", "google"], row.Providers);
        Assert.Equal("andree@example.com", row.Email);
        Assert.True(row.EmailVerified);
    }

    [Fact]
    public async Task Disable_AppliesToEveryLinkedLogin()
    {
        var primary = await SeedLinkedPersonAsync();

        Assert.True(await _svc.SetDisabledAsync(primary.UserId, true));

        var all = await _db.UserIdentities.Where(u => u.UserId == primary.UserId).ToListAsync();
        Assert.Equal(2, all.Count);
        Assert.All(all, u => Assert.True(u.IsDisabled));
    }

    private sealed class StubCatalog : IPlanCatalog
    {
        public void Reload() { }
        public IReadOnlyList<string> Tiers => [];
        public IReadOnlyList<PlanInfo> Plans => [];
        public bool IsValidTier(string tier) => true;
        public PlanInfo? GetPlan(string tier) => null;
    }

    private sealed class NoopDeletion : IAccountDeletionService
    {
        public Task DeleteAsync(UserId userId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
