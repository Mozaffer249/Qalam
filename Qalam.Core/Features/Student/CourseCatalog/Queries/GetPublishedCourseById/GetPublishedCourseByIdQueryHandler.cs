using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCourseById;

public class GetPublishedCourseByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetPublishedCourseByIdQuery, Response<CourseCatalogDetailDto>>
{
    private readonly ICourseRepository _courseRepository;

    public GetPublishedCourseByIdQueryHandler(
        ICourseRepository courseRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _courseRepository = courseRepository;
    }

    public async Task<Response<CourseCatalogDetailDto>> Handle(
        GetPublishedCourseByIdQuery request,
        CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdWithDetailsAsync(request.Id);
        if (course == null)
            return NotFound<CourseCatalogDetailDto>("Course not found.");
        if (course.Status != CourseStatus.Published || !course.IsActive)
            return NotFound<CourseCatalogDetailDto>("Course not found or not available.");

        var dto = new CourseCatalogDetailDto
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            TeacherDisplayName = course.Teacher?.User != null
                ? (course.Teacher.User.FirstName + " " + course.Teacher.User.LastName).Trim()
                : null,
            DomainId = course.TeacherSubject?.Subject?.DomainId ?? 0,
            DomainNameEn = course.TeacherSubject?.Subject?.Domain?.NameEn,
            SubjectId = course.TeacherSubject?.SubjectId ?? 0,
            SubjectNameEn = course.TeacherSubject?.Subject?.NameEn,
            CurriculumId = course.TeacherSubject?.CurriculumId,
            CurriculumNameEn = course.TeacherSubject?.Curriculum?.NameEn,
            LevelId = course.TeacherSubject?.LevelId,
            LevelNameEn = course.TeacherSubject?.Level?.NameEn,
            GradeId = course.TeacherSubject?.GradeId,
            GradeNameEn = course.TeacherSubject?.Grade?.NameEn,
            TeachingModeId = course.TeachingModeId,
            TeachingModeNameEn = course.TeachingMode?.NameEn,
            SessionTypeId = course.SessionTypeId,
            SessionTypeNameEn = course.SessionType?.NameEn,
            IsFlexible = course.IsFlexible,
            SessionsCount = course.SessionsCount,
            SessionDurationMinutes = course.SessionDurationMinutes,
            Price = course.Price,
            MaxStudents = course.MaxStudents,
            AvailableSeats = course.MaxStudents.HasValue
                ? course.MaxStudents.Value - course.CourseEnrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
                : (int?)null
        };

        return Success(entity: dto);
    }
}
