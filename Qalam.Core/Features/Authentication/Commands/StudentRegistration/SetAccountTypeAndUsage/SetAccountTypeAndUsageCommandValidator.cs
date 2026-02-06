using FluentValidation;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class SetAccountTypeAndUsageCommandValidator : AbstractValidator<SetAccountTypeAndUsageCommand>
{
    public SetAccountTypeAndUsageCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.FirstName).NotEmpty().When(x => x.Data != null);
        RuleFor(x => x.Data.LastName).NotEmpty().When(x => x.Data != null);
        RuleFor(x => x.Data.Email).NotEmpty().EmailAddress().When(x => x.Data != null);
        RuleFor(x => x.Data.Password)
            .NotEmpty().MinimumLength(6)
            .When(x => x.Data != null && !string.IsNullOrEmpty(x.Data.PasswordSetupToken));
    }
}
