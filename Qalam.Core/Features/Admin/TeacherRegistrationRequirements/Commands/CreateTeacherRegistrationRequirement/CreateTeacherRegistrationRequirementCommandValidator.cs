using FluentValidation;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.CreateTeacherRegistrationRequirement;

public class CreateTeacherRegistrationRequirementCommandValidator : AbstractValidator<CreateTeacherRegistrationRequirementCommand>
{
    public CreateTeacherRegistrationRequirementCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.Code)
            .NotEmpty()
            .MaximumLength(64)
            .Matches("^[a-z][a-z0-9_]*$")
            .WithMessage("Code must be lowercase snake_case starting with a letter.");
        RuleFor(x => x.Data.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.MinCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Data.MaxCount).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Data).Must(d => d.MaxCount >= d.MinCount).WithMessage("MaxCount must be >= MinCount.");
        RuleFor(x => x.Data.MaxFileSizeBytes).GreaterThan(0).When(x => x.Data.RequirementType == RegistrationRequirementType.File);
        RuleFor(x => x.Data.MaxLength).GreaterThan(0).When(x => x.Data.RequirementType == RegistrationRequirementType.Text);
    }
}
