using FluentValidation;

namespace Qalam.Core.Features.Teacher.TeachingPreferences.Commands.UpdateTeacherTeachingPreferences;

public class UpdateTeacherTeachingPreferencesCommandValidator : AbstractValidator<UpdateTeacherTeachingPreferencesCommand>
{
    public UpdateTeacherTeachingPreferencesCommandValidator()
    {
        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Years of experience must be 0 or greater");

        RuleFor(x => x.JobTitle)
            .MaximumLength(200)
            .When(x => x.JobTitle != null)
            .WithMessage("Job title must be at most 200 characters");

        RuleFor(x => x)
            .Must(x => x.OffersOnline || x.OffersInPerson)
            .WithMessage("At least one delivery mode (online or in-person) must be enabled");

        RuleFor(x => x)
            .Must(x => x.OffersIndividual || x.OffersGroup)
            .WithMessage("At least one session format (individual or group) must be enabled");
    }
}
