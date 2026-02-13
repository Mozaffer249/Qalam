using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Results;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Queries.GetCoursesList;

public class GetCoursesListQueryHandler : ResponseHandler,
    IRequestHandler<GetCoursesListQuery, Response<PaginatedResult<CourseListItemDto>>>
{
    private readonly ITeacherCourseService _teacherCourseService;

    public GetCoursesListQueryHandler(
        ITeacherCourseService teacherCourseService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherCourseService = teacherCourseService;
    }

    public async Task<Response<PaginatedResult<CourseListItemDto>>> Handle(
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
            return Success(entity: result);
        }
        catch (InvalidOperationException)
        {
            return Unauthorized<PaginatedResult<CourseListItemDto>>("Not authorized.");
        }
    }
}
