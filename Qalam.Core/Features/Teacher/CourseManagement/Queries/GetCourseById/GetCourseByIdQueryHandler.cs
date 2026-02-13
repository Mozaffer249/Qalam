using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Queries.GetCourseById;

public class GetCourseByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetCourseByIdQuery, Response<CourseDetailDto>>
{
    private readonly ITeacherCourseService _teacherCourseService;

    public GetCourseByIdQueryHandler(
        ITeacherCourseService teacherCourseService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherCourseService = teacherCourseService;
    }

    public async Task<Response<CourseDetailDto>> Handle(
        GetCourseByIdQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _teacherCourseService.GetCourseByIdForTeacherAsync(request.UserId, request.Id, cancellationToken);
        if (result == null)
            return NotFound<CourseDetailDto>("Course not found.");
        return Success(entity: result);
    }
}
