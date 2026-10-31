using FluentValidation;

namespace Qalam.Core.Features.Student.Commands.SetStudentIdentity;

public class SetStudentIdentityCommandValidator : AbstractValidator<SetStudentIdentityCommand>
{
    public SetStudentIdentityCommandValidator()
    {
        RuleFor(x => x.Identity).NotNull();

        When(x => x.Identity != null, () =>
        {
            RuleFor(x => x.Identity.NationalityId)
                .GreaterThan(0).WithMessage("NationalityId is required.");

            RuleFor(x => x.Identity.IdentityType)
                .IsInEnum().WithMessage("Invalid identity type.");

            RuleFor(x => x.Identity.IdentityNumber)
                .NotEmpty().WithMessage("Identity number is required.")
                .MaximumLength(50).WithMessage("Identity number must not exceed 50 characters.");

            RuleFor(x => x.Identity.IssuingCountryCode)
                .MaximumLength(3).WithMessage("Issuing country code must not exceed 3 characters.")
                .When(x => !string.IsNullOrEmpty(x.Identity.IssuingCountryCode));

            RuleFor(x => x.TargetStudentId)
                .GreaterThan(0).WithMessage("TargetStudentId must be a positive number.")
                .When(x => x.TargetStudentId.HasValue);
        });
    }
}
