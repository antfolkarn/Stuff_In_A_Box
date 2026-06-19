using FluentValidation;

namespace StuffInABox.Application.Boxes.Commands.CreateBox;

public sealed class CreateBoxCommandValidator : AbstractValidator<CreateBoxCommand>
{
    public CreateBoxCommandValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty().WithMessage("Utrymme krävs.");
        RuleFor(x => x.Label).NotEmpty().WithMessage("Lådenamn krävs.").MaximumLength(100);
    }
}
