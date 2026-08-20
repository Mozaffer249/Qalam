using FluentValidation;

namespace Qalam.Core.Features.Teacher.Pricing.Queries.GetCourseHourlyRatePreview;

public class GetCourseHourlyRatePreviewQueryValidator : AbstractValidator<GetCourseHourlyRatePreviewQuery>
{
    public GetCourseHourlyRatePreviewQueryValidator()
    {
        RuleFor(x => x.TeacherSubjectId).GreaterThan(0);
        RuleFor(x => x.SessionTypeId).GreaterThan(0);
        RuleFor(x => x.TotalMinutes).GreaterThan(0).When(x => x.TotalMinutes.HasValue);
    }
}
