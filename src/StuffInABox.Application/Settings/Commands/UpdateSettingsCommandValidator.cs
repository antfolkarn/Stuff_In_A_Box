using FluentValidation;
using StuffInABox.Domain.Entities;

namespace StuffInABox.Application.Settings.Commands;

public sealed class UpdateSettingsCommandValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsCommandValidator()
    {
        RuleFor(x => x.Theme).Must(t => SettingsOptions.Themes.Contains(t))
            .WithMessage("Ogiltigt tema.");
        RuleFor(x => x.Design).Must(d => SettingsOptions.Designs.Contains(d))
            .WithMessage("Ogiltig design.");
        RuleFor(x => x.DisplayName).MaximumLength(UserSettings.MaxDisplayNameLength)
            .WithMessage("Smeknamnet är för långt.");
    }
}
