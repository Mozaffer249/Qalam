using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Teacher.Pricing.Queries.GetCourseHourlyRatePreview;

public class GetCourseHourlyRatePreviewQuery : IRequest<Response<CourseHourlyRatePreviewDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int TeacherSubjectId { get; set; }
    public int SessionTypeId { get; set; }
    public int? TotalMinutes { get; set; }
}
