using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCoursesList;

public class GetPublishedCoursesListQueryHandler : ResponseHandler,
    IRequestHandler<GetPublishedCoursesListQuery, Response<PaginatedResult<CourseCatalogItemDto>>>
{
    private const int DescriptionShortMaxLength = 150;

    private readonly ICourseRepository _courseRepository;
    private readonly IStudentRepository _studentRepository;

    public GetPublishedCoursesListQueryHandler(
        ICourseRepository courseRepository,
        IStudentRepository studentRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _courseRepository = courseRepository;
        _studentRepository = studentRepository;
    }

    public async Task<Response<PaginatedResult<CourseCatalogItemDto>>> Handle(
        GetPublishedCoursesListQuery request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);

        var effectiveDomainId = request.DomainId ?? student?.DomainId;
        var effectiveCurriculumId = request.CurriculumId ?? student?.CurriculumId;
        var effectiveLevelId = request.LevelId ?? student?.LevelId;
        var effectiveGradeId = request.GradeId ?? student?.GradeId;

        var query = _courseRepository.GetPublishedCoursesQueryable();

        if (effectiveDomainId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.Subject != null && c.TeacherSubject.Subject.DomainId == effectiveDomainId.Value);
        if (effectiveCurriculumId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.CurriculumId == effectiveCurriculumId.Value);
        if (effectiveLevelId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.LevelId == effectiveLevelId.Value);
        if (effectiveGradeId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.GradeId == effectiveGradeId.Value);
        if (request.SubjectId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.SubjectId == request.SubjectId.Value);
        if (request.TeachingModeId.HasValue)
            query = query.Where(c => c.TeachingModeId == request.TeachingModeId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseCatalogItemDto
            {
                Id = c.Id,
                Title = c.Title,
                DescriptionShort = c.Description != null && c.Description.Length > DescriptionShortMaxLength
                    ? c.Description.Substring(0, DescriptionShortMaxLength) + "..."
                    : c.Description,
                TeacherDisplayName = c.Teacher != null && c.Teacher.User != null
                    ? (c.Teacher.User.FirstName + " " + c.Teacher.User.LastName).Trim()
                    : null,
                DomainId = c.TeacherSubject != null && c.TeacherSubject.Subject != null ? c.TeacherSubject.Subject.DomainId : 0,
                DomainNameEn = c.TeacherSubject != null && c.TeacherSubject.Subject != null && c.TeacherSubject.Subject.Domain != null ? c.TeacherSubject.Subject.Domain.NameEn : null,
                SubjectId = c.TeacherSubject != null ? c.TeacherSubject.SubjectId : 0,
                SubjectNameEn = c.TeacherSubject != null && c.TeacherSubject.Subject != null ? c.TeacherSubject.Subject.NameEn : null,
                TeachingModeId = c.TeachingModeId,
                TeachingModeNameEn = c.TeachingMode != null ? c.TeachingMode.NameEn : null,
                SessionTypeId = c.SessionTypeId,
                SessionTypeNameEn = c.SessionType != null ? c.SessionType.NameEn : null,
                Price = c.Price,
                MaxStudents = c.MaxStudents,
                AvailableSeats = c.MaxStudents.HasValue
                    ? c.MaxStudents.Value - c.CourseEnrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
                    : (int?)null,
                IsFlexible = c.IsFlexible,
                SessionsCount = c.SessionsCount,
                SessionDurationMinutes = c.SessionDurationMinutes
            })
            .ToListAsync(cancellationToken);

        var result = new PaginatedResult<CourseCatalogItemDto>(items, totalCount, request.PageNumber, request.PageSize);
        return Success(entity: result);
    }
}
