using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Queries.GetCoursesList;

public class GetCoursesListQueryHandler : ResponseHandler,
    IRequestHandler<GetCoursesListQuery, Response<List<CourseListItemDto>>>
{
    private readonly ITeacherCourseService _teacherCourseService;

    public GetCoursesListQueryHandler(
        ITeacherCourseService teacherCourseService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherCourseService = teacherCourseService;
    }

    public async Task<Response<List<CourseListItemDto>>> Handle(
        GetCoursesListQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _teacherCourseService.GetCoursesForTeacherAsync(
                request.UserId,
                request.PageNumber,
                request.PageSize,
                request.DomainId,
                request.Status,
                request.SubjectId,
                cancellationToken);

            return Success(
                entity: result.Items,
                Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
        }
        catch (InvalidOperationException)
        {
            return Unauthorized<List<CourseListItemDto>>("Not authorized.");
        }
    }
}
