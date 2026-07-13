using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Service.Abstracts;

public interface ITeacherSubjectRepertoireService
{
    Task<List<TeacherSubjectUnitOptionDto>?> GetAllowedUnitsForTeacherSubjectAsync(
        int teacherId,
        int teacherSubjectId,
        CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetAllowedUnitIdsAsync(
        TeacherSubject teacherSubject,
        CancellationToken cancellationToken = default);

    Task<string?> ValidateSessionUnitsInRepertoireAsync(
        TeacherSubject teacherSubject,
        IReadOnlyList<CreateCourseSessionDto>? sessions,
        CancellationToken cancellationToken = default);

    Task<string?> ValidateUnitRowsInRepertoireAsync(
        TeacherSubject teacherSubject,
        IReadOnlyList<CreateCourseSessionUnitDto> units,
        CancellationToken cancellationToken = default);
}
