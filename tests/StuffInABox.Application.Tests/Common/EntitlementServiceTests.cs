using Moq;
using StuffInABox.Application.Admin;
using StuffInABox.Application.Common.Services;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Common;

public class EntitlementServiceTests
{
    private readonly Mock<IPlanCatalog> _catalog = new();
    private readonly Mock<IUserSettingsRepository> _settings = new();
    private readonly Mock<ISpaceRepository> _spaces = new();
    private readonly Mock<IItemRepository> _items = new();
    private readonly Mock<ISpaceMembershipRepository> _members = new();
    private readonly UserId _owner = new(Guid.NewGuid());

    private static readonly PlanInfo Free = new("free", 0, MaxSpaces: 1, MaxItems: 100, MaxMembers: 1, AiPhotosPerMonth: 5, StorageMb: 250, ClaudeEnrichment: false, PriorityQueue: false, AllThemes: false);
    private static readonly PlanInfo Pro = new("large", 99, MaxSpaces: -1, MaxItems: -1, MaxMembers: 5, AiPhotosPerMonth: 1000, StorageMb: 10240, ClaudeEnrichment: false, PriorityQueue: true, AllThemes: false);

    public EntitlementServiceTests()
    {
        _catalog.Setup(c => c.GetPlan("free")).Returns(Free);
        _catalog.Setup(c => c.GetPlan("large")).Returns(Pro);
        _catalog.SetupGet(c => c.Plans).Returns([Free, Pro]);
    }

    private EntitlementService Svc() => new(_catalog.Object, _settings.Object, _spaces.Object, _items.Object, _members.Object);

    private void OnTier(string tier)
    {
        var s = UserSettings.CreateDefault(_owner.Value);
        s.SetPlanTier(tier);
        _settings.Setup(r => r.GetAsync(_owner.Value, It.IsAny<CancellationToken>())).ReturnsAsync(s);
    }

    [Fact]
    public async Task Spaces_AtLimit_Throws()
    {
        OnTier("free");
        _spaces.Setup(r => r.CountByOwnerAsync(_owner, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var ex = await Assert.ThrowsAsync<QuotaExceededException>(() => Svc().EnsureCanAddSpaceAsync(_owner));
        Assert.Equal("spaces", ex.Quota);
        Assert.Equal(1, ex.Limit);
        Assert.Equal("free", ex.Plan);
    }

    [Fact]
    public async Task Spaces_UnderLimit_Ok()
    {
        OnTier("free");
        _spaces.Setup(r => r.CountByOwnerAsync(_owner, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        await Svc().EnsureCanAddSpaceAsync(_owner); // does not throw
    }

    [Fact]
    public async Task Items_AtLimit_Throws()
    {
        OnTier("free");
        _items.Setup(r => r.CountByOwnerAsync(_owner, It.IsAny<CancellationToken>())).ReturnsAsync(100);

        await Assert.ThrowsAsync<QuotaExceededException>(() => Svc().EnsureCanAddItemAsync(_owner));
    }

    [Fact]
    public async Task Items_Unlimited_Ok()
    {
        OnTier("large");
        _items.Setup(r => r.CountByOwnerAsync(_owner, It.IsAny<CancellationToken>())).ReturnsAsync(999_999);

        await Svc().EnsureCanAddItemAsync(_owner); // -1 = unlimited
    }

    [Fact]
    public async Task Members_FreeOwner_CannotAddAnyone()
    {
        OnTier("free"); // maxMembers = 1 (owner only)
        var spaceId = Guid.NewGuid();
        _members.Setup(r => r.CountBySpaceAsync(spaceId, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var ex = await Assert.ThrowsAsync<QuotaExceededException>(() => Svc().EnsureCanAddMemberAsync(spaceId, _owner));
        Assert.Equal("members", ex.Quota);
    }

    [Fact]
    public async Task Members_ProAtCap_Throws()
    {
        OnTier("large"); // maxMembers = 5 incl. owner
        var spaceId = Guid.NewGuid();
        _members.Setup(r => r.CountBySpaceAsync(spaceId, It.IsAny<CancellationToken>())).ReturnsAsync(4); // + owner = 5

        await Assert.ThrowsAsync<QuotaExceededException>(() => Svc().EnsureCanAddMemberAsync(spaceId, _owner));
    }

    [Fact]
    public async Task Members_ProUnderCap_Ok()
    {
        OnTier("large");
        var spaceId = Guid.NewGuid();
        _members.Setup(r => r.CountBySpaceAsync(spaceId, It.IsAny<CancellationToken>())).ReturnsAsync(3); // + owner = 4 < 5

        await Svc().EnsureCanAddMemberAsync(spaceId, _owner);
    }

    [Fact]
    public async Task NoSettings_UsesFreeLimits()
    {
        _settings.Setup(r => r.GetAsync(_owner.Value, It.IsAny<CancellationToken>())).ReturnsAsync((UserSettings?)null);
        _spaces.Setup(r => r.CountByOwnerAsync(_owner, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await Assert.ThrowsAsync<QuotaExceededException>(() => Svc().EnsureCanAddSpaceAsync(_owner));
    }

    [Fact]
    public async Task Storage_OverLimit_Throws()
    {
        OnTier("free"); // 250 MB
        _items.Setup(r => r.SumPhotoBytesByOwnerAsync(_owner, It.IsAny<CancellationToken>()))
            .ReturnsAsync(250L * 1024 * 1024);

        var ex = await Assert.ThrowsAsync<QuotaExceededException>(() => Svc().EnsureCanStoreAsync(_owner, 1));
        Assert.Equal("storage", ex.Quota);
        Assert.Equal(250, ex.Limit);
    }

    [Fact]
    public async Task Storage_UnderLimit_Ok()
    {
        OnTier("free");
        _items.Setup(r => r.SumPhotoBytesByOwnerAsync(_owner, It.IsAny<CancellationToken>())).ReturnsAsync(0L);

        await Svc().EnsureCanStoreAsync(_owner, 10L * 1024 * 1024); // 10 MB into a 250 MB plan
    }

    [Fact]
    public async Task Ai_WithinQuota_ConsumesAndReturnsTrue()
    {
        var settings = UserSettings.CreateDefault(_owner.Value);
        settings.SetPlanTier("free"); // 5/month
        _settings.Setup(r => r.GetAsync(_owner.Value, It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var ok = await Svc().TryConsumeAiAsync(_owner);

        Assert.True(ok);
        _settings.Verify(r => r.UpsertAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ai_Exhausted_ReturnsFalse_WithoutConsuming()
    {
        var settings = UserSettings.CreateDefault(_owner.Value);
        settings.SetPlanTier("free");
        var ym = EntitlementService.YearMonth(DateTimeOffset.UtcNow);
        for (var i = 0; i < 5; i++) settings.RecordAiUsage(ym); // Free cap = 5
        _settings.Setup(r => r.GetAsync(_owner.Value, It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var ok = await Svc().TryConsumeAiAsync(_owner);

        Assert.False(ok);
        _settings.Verify(r => r.UpsertAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
