using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using CourseEntity = Qalam.Data.Entity.Course.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.DeleteCourse;

public class DeleteCourseCommandHandler : ResponseHandler,
    IRequestHandler<DeleteCourseCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseRepository _courseRepository;

    public DeleteCourseCommandHandler(
        ITeacherRepository teacherRepository,
        ICourseRepository courseRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _courseRepository = courseRepository;
    }

    public async Task<Response<string>> Handle(
        DeleteCourseCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return Unauthorized<string>("Not authorized.");
        
        if (teacher.Status != TeacherStatus.Active)
            return BadRequest<string>("Teacher account is not active.");

        var course = await _courseRepository.GetByIdAsync(request.Id) as CourseEntity;
        if (course == null)
            return NotFound<string>("Course not found.");
        if (course.TeacherId != teacher.Id)
            return Forbidden<string>("Access denied.");

        var hasEnrollments = await _courseRepository.HasEnrollmentsAsync(course.Id);

        if (hasEnrollments)
        {
            course.IsActive = false;
            course.Status = CourseStatus.Paused;
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.UpdateAsync(course);
            await _courseRepository.SaveChangesAsync();
            return Success<string>(Message: "Course deactivated (has enrollments).");
        }

        await _courseRepository.DeleteAsync(course);
        await _courseRepository.SaveChangesAsync();
        return Success<string>(Message: "Course deleted.");
    }
}
