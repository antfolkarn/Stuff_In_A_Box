using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Settings.Queries;

public sealed class GetSettingsQueryHandler(
    IUserSettingsRepository repo,
    ICurrentUserService currentUser)
    : IRequestHandler<GetSettingsQuery, SettingsDto>
{
    public async Task<SettingsDto> Handle(GetSettingsQuery request, CancellationToken ct)
    {
        var settings = await repo.GetAsync(currentUser.UserId.Value, ct);
        return settings is null
            ? new SettingsDto(SettingsOptions.DefaultTheme, SettingsOptions.DefaultDesign, null)
            : new SettingsDto(settings.Theme, settings.Design, settings.DisplayName);
    }
}
