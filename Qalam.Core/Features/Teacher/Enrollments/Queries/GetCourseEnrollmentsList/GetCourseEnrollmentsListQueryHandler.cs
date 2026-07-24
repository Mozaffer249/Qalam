using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetCourseEnrollmentsList;

public class GetCourseEnrollmentsListQueryHandler : ResponseHandler,
    IRequestHandler<GetCourseEnrollmentsListQuery, Response<List<TeacherEnrollmentListItemDto>>>
{
    private readonly ITeacherEnrollmentService _enrollmentService;

    public GetCourseEnrollmentsListQueryHandler(
        ITeacherEnrollmentService enrollmentService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentService = enrollmentService;
    }

    public async Task<Response<List<TeacherEnrollmentListItemDto>>> Handle(
        GetCourseEnrollmentsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.GetCourseEnrollmentsAsync(
            request.UserId,
            request.CourseId,
            request.Status,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        if (result == null)
            return NotFound<List<TeacherEnrollmentListItemDto>>("Course not found or does not belong to you.");

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
