using Moq;
using StuffInABox.Application.Admin;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Application.Subscription.Queries;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Subscription;

public class GetSubscriptionHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUserSettingsRepository> _settingsRepo = new();
    private readonly Mock<ISpaceRepository> _spaceRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IPlanCatalog> _catalog = new();
    private readonly UserId _userId = new(Guid.NewGuid());

    private static readonly PlanInfo Free = new("free", 0, MaxSpaces: 1, MaxItems: 100, MaxMembers: 1, AiPhotosPerMonth: 20, StorageMb: 250, ClaudeEnrichment: false, PriorityQueue: false, AllThemes: false);
    private static readonly PlanInfo Large = new("large", 99, MaxSpaces: -1, MaxItems: -1, MaxMembers: -1, AiPhotosPerMonth: 5000, StorageMb: 50000, ClaudeEnrichment: true, PriorityQueue: true, AllThemes: true);

    public GetSubscriptionHandlerTests()
    {
        _currentUser.SetupGet(c => c.UserId).Returns(_userId);
        _catalog.SetupGet(c => c.Plans).Returns([Free, Large]);
        _catalog.Setup(c => c.GetPlan(It.IsAny<string>()))
            .Returns((string t) => t == "large" ? Large : t == "free" ? Free : null);
        _spaceRepo.Setup(r => r.GetAllAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Space>());
        _itemRepo.Setup(r => r.GetByOwnerAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Item>());
    }

    private GetSubscriptionQueryHandler Handler() =>
        new(_currentUser.Object, _settingsRepo.Object, _spaceRepo.Object, _itemRepo.Object, _catalog.Object);

    [Fact]
    public async Task Handle_NoSettings_DefaultsToFree()
    {
        _settingsRepo.Setup(r => r.GetAsync(_userId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSettings?)null);

        var result = await Handler().Handle(new GetSubscriptionQuery(), default);

        Assert.Equal("free", result.Tier);
        Assert.Equal(0, result.PriceSek);
        Assert.True(result.Plans.Single(p => p.Tier == "free").Current);
        Assert.False(result.Plans.Single(p => p.Tier == "large").Current);
    }

    [Fact]
    public async Task Handle_ReportsUsageAgainstCurrentPlanLimits()
    {
        var settings = UserSettings.CreateDefault(_userId.Value);
        settings.SetPlanTier("large");
        _settingsRepo.Setup(r => r.GetAsync(_userId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        // Two spaces owned; count is all we use, so placeholder entries are fine.
        _spaceRepo.Setup(r => r.GetAllAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Space[2]);

        var result = await Handler().Handle(new GetSubscriptionQuery(), default);

        Assert.Equal("large", result.Tier);
        Assert.Equal(99, result.PriceSek);
        Assert.Equal(2, result.Usage.Spaces);
        Assert.Equal(-1, result.Usage.MaxSpaces); // unlimited on large
        Assert.True(result.Plans.Single(p => p.Tier == "large").Current);
    }

    [Fact]
    public async Task Handle_UnknownStoredTier_FallsBackToFirstPlan()
    {
        var settings = UserSettings.CreateDefault(_userId.Value);
        settings.SetPlanTier("legacy-tier");
        _settingsRepo.Setup(r => r.GetAsync(_userId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var result = await Handler().Handle(new GetSubscriptionQuery(), default);

        Assert.Equal("free", result.Tier); // first plan in the catalog
        Assert.True(result.Plans.Single(p => p.Tier == "free").Current);
    }
}
