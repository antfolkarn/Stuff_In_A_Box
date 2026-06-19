using FluentValidation;

namespace StuffInABox.Application.Spaces.Commands.CreateSpace;

public sealed class CreateSpaceCommandValidator : AbstractValidator<CreateSpaceCommand>
{
    public CreateSpaceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Namn krävs.")
            .MaximumLength(100).WithMessage("Namn får vara max 100 tecken.");

        RuleFor(x => x.Icon)
            .NotEmpty().WithMessage("Ikon krävs.");
    }
}
