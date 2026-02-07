using FluentValidation;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class SetAccountTypeAndUsageCommandValidator : AbstractValidator<SetAccountTypeAndUsageCommand>
{
    public SetAccountTypeAndUsageCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        
        RuleFor(x => x.Data.AccountType)
            .NotEmpty()
            .WithMessage("AccountType is required")
            .Must(value => new[] { "student", "parent", "both" }.Contains(value?.ToLower()))
            .WithMessage("AccountType must be: Student, Parent, or Both")
            .When(x => x.Data != null);
        
        RuleFor(x => x.Data.UsageMode)
            .Must((cmd, usageMode) => 
            {
                if (cmd.Data == null) return true;
                
                var accountType = cmd.Data.AccountType?.ToLower();
                if (accountType == "parent" || accountType == "both")
                {
                    return !string.IsNullOrEmpty(usageMode) && 
                           new[] { "studyself", "addchildren", "both" }.Contains(usageMode.ToLower());
                }
                return true;
            })
            .WithMessage("UsageMode is required for Parent or Both account types. Valid values: StudySelf, AddChildren, Both");
        
        RuleFor(x => x.Data.FirstName).NotEmpty().When(x => x.Data != null);
        RuleFor(x => x.Data.LastName).NotEmpty().When(x => x.Data != null);
        RuleFor(x => x.Data.Email).NotEmpty().EmailAddress().When(x => x.Data != null);
        RuleFor(x => x.Data.Password)
            .NotEmpty().MinimumLength(6)
            .When(x => x.Data != null);
    }
}
