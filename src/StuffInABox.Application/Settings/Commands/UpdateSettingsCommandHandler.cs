using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Settings.Commands;

public sealed class UpdateSettingsCommandHandler(
    IUserSettingsRepository repo,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateSettingsCommand, SettingsDto>
{
    public async Task<SettingsDto> Handle(UpdateSettingsCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId.Value;
        var settings = await repo.GetAsync(userId, ct) ?? UserSettings.CreateDefault(userId);
        settings.Update(request.Theme, request.Design, request.DisplayName);
        await repo.UpsertAsync(settings, ct);
        return new SettingsDto(settings.Theme, settings.Design, settings.DisplayName);
    }
}
