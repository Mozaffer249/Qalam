using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Qalam.Infrastructure.context;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class CompleteStudentProfileCommandValidator : AbstractValidator<CompleteStudentProfileCommand>
{
    public CompleteStudentProfileCommandValidator(ApplicationDBContext db)
    {
        RuleFor(x => x.Profile).NotNull();
        RuleFor(x => x.Profile.DomainId).GreaterThan(0).When(x => x.Profile != null);

        RuleFor(x => x.Profile)
            .CustomAsync(async (profile, context, ct) =>
            {
                if (profile == null) return;

                var domain = await db.EducationDomains.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == profile.DomainId, ct);
                if (domain == null)
                {
                    context.AddFailure(nameof(profile.DomainId), "Domain not found");
                    return;
                }

                if (string.Equals(domain.Code, "university", StringComparison.OrdinalIgnoreCase))
                {
                    if (!profile.UniversityId.HasValue || profile.UniversityId <= 0)
                        context.AddFailure(nameof(profile.UniversityId), "University is required for university domain");
                    if (!profile.CollegeId.HasValue || profile.CollegeId <= 0)
                        context.AddFailure(nameof(profile.CollegeId), "College is required for university domain");
                    if (!profile.DepartmentId.HasValue || profile.DepartmentId <= 0)
                        context.AddFailure(nameof(profile.DepartmentId), "Department is required for university domain");
                    if (!profile.AcademicProgramId.HasValue || profile.AcademicProgramId <= 0)
                        context.AddFailure(nameof(profile.AcademicProgramId), "Academic program is required for university domain");
                    if (!profile.LevelId.HasValue || profile.LevelId <= 0)
                        context.AddFailure(nameof(profile.LevelId), "Academic level is required for university domain");
                }
            });
    }
}
