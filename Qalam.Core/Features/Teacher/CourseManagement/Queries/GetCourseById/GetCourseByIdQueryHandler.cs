using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Course;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Queries.GetCourseById;

public class GetCourseByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetCourseByIdQuery, Response<CourseDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseRepository _courseRepository;

    public GetCourseByIdQueryHandler(
        ITeacherRepository teacherRepository,
        ICourseRepository courseRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _courseRepository = courseRepository;
    }

    public async Task<Response<CourseDetailDto>> Handle(
        GetCourseByIdQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return Unauthorized<CourseDetailDto>("Not authorized.");

        var course = await _courseRepository.GetByIdWithDetailsAsync(request.Id);
        if (course == null)
            return NotFound<CourseDetailDto>("Course not found.");
        if (course.TeacherId != teacher.Id)
            return Forbidden<CourseDetailDto>("Access denied.");

        return Success(entity: CourseDtoMapper.MapToDetailDto(course));
    }
}
