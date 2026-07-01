using Microsoft.EntityFrameworkCore;
using StuffInABox.Application.Admin;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;
using StuffInABox.Infrastructure.Admin;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Infrastructure.Tests.Admin;

public class AdminServiceDeleteUserTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FakeDeletion _deletion = new();
    private readonly AdminService _svc;

    public AdminServiceDeleteUserTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options;
        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _svc = new AdminService(_db, new StubCatalog(), _deletion);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task DeleteUser_Existing_RunsCascadeForThatUser()
    {
        var identity = UserIdentity.CreateOAuth("google", "sub-1", "u@e.com");
        _db.UserIdentities.Add(identity);
        await _db.SaveChangesAsync();

        var ok = await _svc.DeleteUserAsync(identity.InternalUserId);

        Assert.True(ok);
        Assert.Equal(identity.InternalUserId, _deletion.DeletedUserId);
    }

    [Fact]
    public async Task DeleteUser_Missing_ReturnsFalse_AndSkipsCascade()
    {
        var ok = await _svc.DeleteUserAsync(Guid.NewGuid());

        Assert.False(ok);
        Assert.Null(_deletion.DeletedUserId);
    }

    private sealed class FakeDeletion : IAccountDeletionService
    {
        public Guid? DeletedUserId;
        public Task DeleteAsync(UserId userId, CancellationToken ct = default)
        {
            DeletedUserId = userId.Value;
            return Task.CompletedTask;
        }
    }

    private sealed class StubCatalog : IPlanCatalog
    {
        public void Reload() { }
        public IReadOnlyList<string> Tiers => [];
        public IReadOnlyList<PlanInfo> Plans => [];
        public bool IsValidTier(string tier) => true;
        public PlanInfo? GetPlan(string tier) => null;
    }
}
