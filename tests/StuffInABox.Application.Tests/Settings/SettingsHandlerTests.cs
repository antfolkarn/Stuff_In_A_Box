using Moq;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Application.Settings.Commands;
using StuffInABox.Application.Settings.Queries;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Settings;

public class SettingsHandlerTests
{
    private readonly Mock<IUserSettingsRepository> _repo = new();
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly UserId _userId = new(Guid.NewGuid());

    public SettingsHandlerTests() => _user.Setup(u => u.UserId).Returns(_userId);

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

        var result = await new UpdateSettingsCommandHandler(_repo.Object, _user.Object)
            .Handle(new UpdateSettingsCommand("dark", "pop"), default);

        Assert.Equal("dark", result.Theme);
        Assert.Equal("pop", result.Design);
        Assert.NotNull(saved);
        Assert.Equal(_userId.Value, saved!.UserId);
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
