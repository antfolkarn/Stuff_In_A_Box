using FluentValidation;

namespace StuffInABox.Application.Items.Commands.CreateItemFromPhoto;

public sealed class CreateItemFromPhotoCommandValidator : AbstractValidator<CreateItemFromPhotoCommand>
{
    public CreateItemFromPhotoCommandValidator()
    {
        RuleFor(x => x.BoxNumber).GreaterThan(0).WithMessage("Lådenummer måste vara >= 1.");
        RuleFor(x => x.Content).NotEmpty().WithMessage("Bild krävs.");
    }
}
