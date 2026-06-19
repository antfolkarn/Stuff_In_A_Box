using FluentValidation;

namespace StuffInABox.Application.Items.Commands.AddItem;

public sealed class AddItemCommandValidator : AbstractValidator<AddItemCommand>
{
    public AddItemCommandValidator()
    {
        RuleFor(x => x.BoxNumber).GreaterThan(0).WithMessage("Lådenummer måste vara >= 1.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Namn krävs.").MaximumLength(200);
    }
}
