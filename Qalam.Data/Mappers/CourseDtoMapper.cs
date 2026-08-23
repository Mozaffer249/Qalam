using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Data.Mappers;

public static class CourseDtoMapper
{
    public static CourseDetailDto MapToDetailDto(Course c)
    {
        var dto = new CourseDetailDto
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            IsActive = c.IsActive,
            TeacherId = c.TeacherId,
            TeacherDisplayName = c.Teacher?.User != null
                ? $"{c.Teacher.User.FirstName} {c.Teacher.User.LastName}".Trim()
                : null,
            DomainId = c.DomainId,
            DomainNameEn = c.Domain?.NameEn,
            DomainNameAr = c.Domain?.NameAr,
            TeacherSubjectId = c.TeacherSubjectId,
            SubjectNameEn = c.Subject?.NameEn,
            SubjectNameAr = c.Subject?.NameAr,
            CurriculumId = c.CurriculumId,
            CurriculumNameEn = c.Curriculum?.NameEn,
            CurriculumNameAr = c.Curriculum?.NameAr,
            LevelId = c.LevelId,
            LevelNameEn = c.Level?.NameEn,
            LevelNameAr = c.Level?.NameAr,
            GradeId = c.GradeId,
            GradeNameEn = c.Grade?.NameEn,
            GradeNameAr = c.Grade?.NameAr,
            TeachingModeId = c.TeachingModeId,
            TeachingModeNameEn = c.TeachingMode?.NameEn,
            TeachingModeNameAr = c.TeachingMode?.NameAr,
            SessionTypeId = c.SessionTypeId,
            SessionTypeNameEn = c.SessionType?.NameEn,
            SessionTypeNameAr = c.SessionType?.NameAr,
            IsFlexible = c.IsFlexible,
            SessionsCount = c.SessionsCount,
            SessionDurationMinutes = c.SessionDurationMinutes,
            Price = c.Price,
            MaxStudents = c.MaxStudents,
            CanIncludeInPackages = c.CanIncludeInPackages,
            ImageUrl = c.ImageUrl,
            Status = c.Status,
            HasBlockingEnrollments = false,
            CanEdit = c.Status != CourseStatus.Paused
        };

        if (c.TeacherSubject?.CanTeachFullSubject == false &&
            c.TeacherSubject.TeacherSubjectUnits?.Count > 0)
        {
            dto.Units = c.TeacherSubject.TeacherSubjectUnits
                .Select(MapTeacherSubjectUnitToDto)
                .ToList();
        }

        if (c.Sessions != null && c.Sessions.Count > 0)
        {
            dto.Sessions = c.Sessions
                .OrderBy(s => s.SessionNumber)
                .Select(s => new CourseSessionDto
                {
                    Id = s.Id,
                    SessionNumber = s.SessionNumber,
                    DurationMinutes = s.DurationMinutes,
                    Title = s.Title,
                    Notes = s.Notes,
                    QuranContentTypeId = s.QuranContentTypeId,
                    QuranLevelId = s.QuranLevelId,
                    Units = s.Units != null
                        ? s.Units.Select(MapCourseSessionUnitToDto).ToList()
                        : new List<CourseSessionUnitDto>()
                })
                .ToList();
        }

        return dto;
    }

    public static CourseSessionUnitDto MapCourseSessionUnitToDto(CourseSessionUnit u)
    {
        return new CourseSessionUnitDto
        {
            Id = u.Id,
            ContentUnitId = u.ContentUnitId,
            ContentUnitNameEn = u.ContentUnit?.NameEn,
            ContentUnitNameAr = u.ContentUnit?.NameAr,
            LessonId = u.LessonId,
            LessonNameEn = u.Lesson?.NameEn,
            LessonNameAr = u.Lesson?.NameAr,
            CustomUnitLabel = u.CustomUnitLabel
        };
    }

    private static TeacherSubjectUnitResponseDto MapTeacherSubjectUnitToDto(TeacherSubjectUnit tsu)
    {
        return new TeacherSubjectUnitResponseDto
        {
            Id = tsu.Id,
            UnitId = tsu.UnitId,
            UnitNameAr = tsu.Unit?.NameAr ?? "",
            UnitNameEn = tsu.Unit?.NameEn ?? "",
            UnitTypeCode = tsu.Unit?.UnitTypeCode
        };
    }

    public static CourseListItemDto MapToListItemDto(Course c)
    {
        var sessions = c.Sessions ?? Array.Empty<CourseSession>();
        var sessionsCount = c.IsFlexible
            ? (int?)null
            : sessions.Count;
        var totalMinutes = sessions.Sum(s => s.DurationMinutes);
        var registeredCount = c.Enrollments?.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active) ?? 0;
        // Include may only load Active; treat any loaded Active/Completed as blocking.
        // Callers that need Completed must ensure those enrollments are loaded or set flags after HasEnrollmentsAsync.
        var hasBlockingFromNav = c.Enrollments != null && c.Enrollments.Any(e =>
            e.EnrollmentStatus == EnrollmentStatus.Active
            || e.EnrollmentStatus == EnrollmentStatus.Completed);
        var canEdit = c.Status != CourseStatus.Paused && !hasBlockingFromNav;

        return new CourseListItemDto
        {
            Id = c.Id,
            Title = c.Title,
            DescriptionShort = c.Description?.Length > 200
                ? c.Description.Substring(0, 200) + "..."
                : c.Description,
            TeacherId = c.TeacherId,
            DomainId = c.DomainId,
            DomainNameEn = c.Domain?.NameEn,
            DomainNameAr = c.Domain?.NameAr,
            SubjectId = c.SubjectId,
            SubjectNameEn = c.Subject?.NameEn,
            SubjectNameAr = c.Subject?.NameAr,
            TeachingModeId = c.TeachingModeId,
            TeachingModeNameEn = c.TeachingMode?.NameEn,
            TeachingModeNameAr = c.TeachingMode?.NameAr,
            SessionTypeId = c.SessionTypeId,
            SessionTypeNameEn = c.SessionType?.NameEn,
            SessionTypeNameAr = c.SessionType?.NameAr,
            Status = c.Status,
            IsActive = c.IsActive,
            Price = c.Price,
            SessionsCount = sessionsCount,
            TotalMinutes = totalMinutes,
            RegisteredCount = registeredCount,
            MaxStudents = c.MaxStudents,
            HasBlockingEnrollments = hasBlockingFromNav,
            CanEdit = canEdit
        };
    }
}
