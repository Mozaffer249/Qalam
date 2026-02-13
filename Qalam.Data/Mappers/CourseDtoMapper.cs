using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Teacher;
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

        if (c.TeacherSubject?.CanTeachFullSubject == false &&
            c.TeacherSubject.TeacherSubjectUnits?.Count > 0)
        {
            dto.Units = c.TeacherSubject.TeacherSubjectUnits
                .Select(MapTeacherSubjectUnitToDto)
                .ToList();
        }

        return dto;
    }

    private static TeacherSubjectUnitResponseDto MapTeacherSubjectUnitToDto(TeacherSubjectUnit tsu)
    {
        return new TeacherSubjectUnitResponseDto
        {
            Id = tsu.Id,
            UnitId = tsu.UnitId,
            UnitNameAr = tsu.Unit?.NameAr ?? "",
            UnitNameEn = tsu.Unit?.NameEn ?? "",
            UnitTypeCode = tsu.Unit?.UnitTypeCode,
            QuranContentTypeId = tsu.QuranContentTypeId,
            QuranContentTypeNameAr = tsu.QuranContentType?.NameAr,
            QuranContentTypeNameEn = tsu.QuranContentType?.NameEn,
            QuranLevelId = tsu.QuranLevelId,
            QuranLevelNameAr = tsu.QuranLevel?.NameAr,
            QuranLevelNameEn = tsu.QuranLevel?.NameEn
        };
    }

    public static CourseListItemDto MapToListItemDto(Course c)
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
