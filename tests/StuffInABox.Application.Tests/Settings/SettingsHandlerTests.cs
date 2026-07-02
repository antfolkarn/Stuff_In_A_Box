using Moq;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Application.Settings.Commands;
using StuffInABox.Application.Settings.Queries;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Settings;

public class SettingsHandlerTests
{
    private readonly Mock<IUserSettingsRepository> _repo = new();
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly Mock<IEntitlementService> _entitlements = new();
    private readonly UserId _userId = new(Guid.NewGuid());

    public SettingsHandlerTests() => _user.Setup(u => u.UserId).Returns(_userId);

    private UpdateSettingsCommandHandler UpdateHandler() =>
        new(_repo.Object, _user.Object, _entitlements.Object);

    [Fact]
    public async Task Get_NoStoredSettings_ReturnsDefaults()
    {
        _repo.Setup(r => r.GetAsync(_userId.Value, It.IsAny<CancellationToken>())).ReturnsAsync((UserSettings?)null);

        var result = await new GetSettingsQueryHandler(_repo.Object, _user.Object)
            .Handle(new GetSettingsQuery(), default);

        Assert.Equal("system", result.Theme);
        Assert.Equal("standard", result.Design);
    }

    [Fact]
    public async Task Update_UpsertsAndReturnsValues()
    {
        _repo.Setup(r => r.GetAsync(_userId.Value, It.IsAny<CancellationToken>())).ReturnsAsync((UserSettings?)null);
        UserSettings? saved = null;
        _repo.Setup(r => r.UpsertAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>()))
             .Callback<UserSettings, CancellationToken>((s, _) => saved = s)
             .Returns(Task.CompletedTask);

        var result = await UpdateHandler()
            .Handle(new UpdateSettingsCommand("dark", "pop"), default);

        Assert.Equal("dark", result.Theme);
        Assert.Equal("pop", result.Design);
        Assert.NotNull(saved);
        Assert.Equal(_userId.Value, saved!.UserId);
    }

    [Fact]
    public async Task Update_StoresTrimmedDisplayName()
    {
        _repo.Setup(r => r.GetAsync(_userId.Value, It.IsAny<CancellationToken>())).ReturnsAsync((UserSettings?)null);
        _repo.Setup(r => r.UpsertAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await UpdateHandler()
            .Handle(new UpdateSettingsCommand("system", "standard", "  Anna  "), default);

        Assert.Equal("Anna", result.DisplayName);
    }

    [Fact]
    public async Task Update_BlankDisplayName_ClearsToNull()
    {
        _repo.Setup(r => r.GetAsync(_userId.Value, It.IsAny<CancellationToken>())).ReturnsAsync((UserSettings?)null);
        _repo.Setup(r => r.UpsertAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await UpdateHandler()
            .Handle(new UpdateSettingsCommand("system", "standard", "   "), default);

        Assert.Null(result.DisplayName);
    }

    [Fact]
    public async Task Update_PremiumDesign_FreePlan_Throws()
    {
        _repo.Setup(r => r.GetAsync(_userId.Value, It.IsAny<CancellationToken>())).ReturnsAsync((UserSettings?)null);
        _entitlements.Setup(e => e.HasAllThemesAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<QuotaExceededException>(
            () => UpdateHandler().Handle(new UpdateSettingsCommand("system", "atelier"), default));
        Assert.Equal("themes", ex.Quota);
        _repo.Verify(r => r.UpsertAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_PremiumDesign_WithAllThemes_Succeeds()
    {
        _repo.Setup(r => r.GetAsync(_userId.Value, It.IsAny<CancellationToken>())).ReturnsAsync((UserSettings?)null);
        _repo.Setup(r => r.UpsertAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _entitlements.Setup(e => e.HasAllThemesAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await UpdateHandler().Handle(new UpdateSettingsCommand("system", "atelier"), default);

        Assert.Equal("atelier", result.Design);
    }

    [Fact]
    public async Task Update_KeepsGrandfatheredPremiumDesign_DoesNotThrow()
    {
        // Already on a premium design (e.g. after a downgrade) — an unrelated change must not fail.
        var existing = UserSettings.CreateDefault(_userId.Value);
        existing.Update("system", "atelier", null);
        _repo.Setup(r => r.GetAsync(_userId.Value, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repo.Setup(r => r.UpsertAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _entitlements.Setup(e => e.HasAllThemesAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await UpdateHandler().Handle(new UpdateSettingsCommand("dark", "atelier", "Anna"), default);

        Assert.Equal("atelier", result.Design);
        Assert.Equal("Anna", result.DisplayName);
    }

    [Fact]
    public void Validator_RejectsTooLongDisplayName()
    {
        var result = new UpdateSettingsCommandValidator()
            .Validate(new UpdateSettingsCommand("system", "standard", new string('x', 41)));
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("neon", "standard")]
    [InlineData("dark", "brutalist")]
    public void Validator_RejectsUnknownValues(string theme, string design)
    {
        var result = new UpdateSettingsCommandValidator().Validate(new UpdateSettingsCommand(theme, design));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_AcceptsKnownValues()
    {
        var result = new UpdateSettingsCommandValidator().Validate(new UpdateSettingsCommand("system", "atelier"));
        Assert.True(result.IsValid);
    }
}
