using FluentValidation;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class CompleteStudentProfileCommandValidator : AbstractValidator<CompleteStudentProfileCommand>
{
    public CompleteStudentProfileCommandValidator()
    {
        RuleFor(x => x.Profile).NotNull();
        RuleFor(x => x.Profile.DomainId).GreaterThan(0).When(x => x.Profile != null);
    }
}
