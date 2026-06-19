using FluentValidation;

namespace StuffInABox.Application.Boxes.Commands.UpdateBoxLabel;

public sealed class UpdateBoxLabelCommandValidator : AbstractValidator<UpdateBoxLabelCommand>
{
    public UpdateBoxLabelCommandValidator()
    {
        RuleFor(x => x.BoxNumber).GreaterThan(0);
        RuleFor(x => x.Label).NotEmpty().WithMessage("Lådenamn krävs.").MaximumLength(100);
    }
}
