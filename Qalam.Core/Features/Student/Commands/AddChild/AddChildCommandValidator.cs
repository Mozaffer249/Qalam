using FluentValidation;

namespace Qalam.Core.Features.Student.Commands.AddChild;

public class AddChildCommandValidator : AbstractValidator<AddChildCommand>
{
    public AddChildCommandValidator()
    {
        RuleFor(x => x.Child).NotNull();
        RuleFor(x => x.Child.FullName).NotEmpty().When(x => x.Child != null);
    }
}
