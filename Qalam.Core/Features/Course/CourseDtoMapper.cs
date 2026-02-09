using Qalam.Data.DTOs.Course;
using CourseEntity = Qalam.Data.Entity.Course.Course;

namespace Qalam.Core.Features.Course;

internal static class CourseDtoMapper
{
    public static CourseDetailDto MapToDetailDto(CourseEntity c, string? domainName = null, string? subjectName = null, string? teachingModeName = null, string? sessionTypeName = null, int? teacherId = null, string? teacherDisplayName = null)
    {
        return new CourseDetailDto
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            IsActive = c.IsActive,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            TeacherId = teacherId ?? c.TeacherId,
            TeacherDisplayName = teacherDisplayName ?? (c.Teacher?.User != null ? $"{c.Teacher.User.FirstName} {c.Teacher.User.LastName}".Trim() : null),
            DomainId = c.DomainId,
            DomainNameEn = domainName ?? c.Domain?.NameEn,
            SubjectId = c.SubjectId,
            SubjectNameEn = subjectName ?? c.Subject?.NameEn,
            CurriculumId = c.CurriculumId,
            CurriculumNameEn = c.Curriculum?.NameEn,
            LevelId = c.LevelId,
            LevelNameEn = c.Level?.NameEn,
            GradeId = c.GradeId,
            GradeNameEn = c.Grade?.NameEn,
            TeachingModeId = c.TeachingModeId,
            TeachingModeNameEn = teachingModeName ?? c.TeachingMode?.NameEn,
            SessionTypeId = c.SessionTypeId,
            SessionTypeNameEn = sessionTypeName ?? c.SessionType?.NameEn,
            IsFlexible = c.IsFlexible,
            SessionsCount = c.SessionsCount,
            SessionDurationMinutes = c.SessionDurationMinutes,
            Price = c.Price,
            MaxStudents = c.MaxStudents,
            CanIncludeInPackages = c.CanIncludeInPackages,
            Status = c.Status
        };
    }
}
