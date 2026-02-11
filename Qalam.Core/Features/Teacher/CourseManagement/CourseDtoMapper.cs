using Qalam.Data.DTOs.Course;
using CourseEntity = Qalam.Data.Entity.Course.Course;

namespace Qalam.Core.Features.Teacher.CourseManagement;

internal static class CourseDtoMapper
{
    public static CourseDetailDto MapToDetailDto(CourseEntity c)
    {
        return new CourseDetailDto
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
            SubjectId = c.SubjectId,
            SubjectNameEn = c.Subject?.NameEn,
            CurriculumId = c.CurriculumId,
            CurriculumNameEn = c.Curriculum?.NameEn,
            LevelId = c.LevelId,
            LevelNameEn = c.Level?.NameEn,
            GradeId = c.GradeId,
            GradeNameEn = c.Grade?.NameEn,
            TeachingModeId = c.TeachingModeId,
            TeachingModeNameEn = c.TeachingMode?.NameEn,
            SessionTypeId = c.SessionTypeId,
            SessionTypeNameEn = c.SessionType?.NameEn,
            IsFlexible = c.IsFlexible,
            SessionsCount = c.SessionsCount,
            SessionDurationMinutes = c.SessionDurationMinutes,
            Price = c.Price,
            MaxStudents = c.MaxStudents,
            CanIncludeInPackages = c.CanIncludeInPackages,
            Status = c.Status
        };
    }

    public static CourseListItemDto MapToListItemDto(CourseEntity c)
    {
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
            SubjectId = c.SubjectId,
            SubjectNameEn = c.Subject?.NameEn,
            TeachingModeId = c.TeachingModeId,
            TeachingModeNameEn = c.TeachingMode?.NameEn,
            SessionTypeId = c.SessionTypeId,
            SessionTypeNameEn = c.SessionType?.NameEn,
            Status = c.Status,
            IsActive = c.IsActive,
            Price = c.Price
        };
    }
}
