using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Settings.Commands;

public sealed class UpdateSettingsCommandHandler(
    IUserSettingsRepository repo,
    ICurrentUserService currentUser,
    IEntitlementService entitlements)
    : IRequestHandler<UpdateSettingsCommand, SettingsDto>
{
    public async Task<SettingsDto> Handle(UpdateSettingsCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId.Value;
        var settings = await repo.GetAsync(userId, ct) ?? UserSettings.CreateDefault(userId);

        // Gate premium designs behind the plan's AllThemes flag. Only block a *switch* to a new
        // premium design — keeping one already selected is grandfathered, so an unrelated change
        // (nickname/theme) never fails just because the current design is now above the plan.
        var changingDesign = !string.Equals(settings.Design, request.Design, StringComparison.Ordinal);
        if (changingDesign
            && !SettingsOptions.FreeDesigns.Contains(request.Design)
            && !await entitlements.HasAllThemesAsync(currentUser.UserId, ct))
        {
            throw new QuotaExceededException("themes", 0, settings.PlanTier);
        }

        settings.Update(request.Theme, request.Design, request.DisplayName);
        await repo.UpsertAsync(settings, ct);
        return new SettingsDto(settings.Theme, settings.Design, settings.DisplayName);
    }
}
