using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherSubjectRepertoireService : ITeacherSubjectRepertoireService
{
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly IContentUnitRepository _contentUnitRepository;
    private readonly ILessonRepository _lessonRepository;

    public TeacherSubjectRepertoireService(
        ITeacherSubjectRepository teacherSubjectRepository,
        IContentUnitRepository contentUnitRepository,
        ILessonRepository lessonRepository)
    {
        _teacherSubjectRepository = teacherSubjectRepository;
        _contentUnitRepository = contentUnitRepository;
        _lessonRepository = lessonRepository;
    }

    public async Task<List<TeacherSubjectUnitOptionDto>?> GetAllowedUnitsForTeacherSubjectAsync(
        int teacherId,
        int teacherSubjectId,
        CancellationToken cancellationToken = default)
    {
        var teacherSubject = await _teacherSubjectRepository.GetByIdForTeacherAsync(
            teacherId, teacherSubjectId, cancellationToken);

        if (teacherSubject == null
            || !teacherSubject.IsActive
            || teacherSubject.VerificationStatus != DocumentVerificationStatus.Approved)
        {
            return null;
        }

        return await MapAllowedUnitsAsync(teacherSubject, cancellationToken);
    }

    public async Task<HashSet<int>> GetAllowedUnitIdsAsync(
        TeacherSubject teacherSubject,
        CancellationToken cancellationToken = default)
    {
        if (teacherSubject.CanTeachFullSubject)
        {
            var ids = await _contentUnitRepository
                .GetContentUnitsBySubjectId(teacherSubject.SubjectId)
                .Where(cu => cu.IsActive)
                .Select(cu => cu.Id)
                .ToListAsync(cancellationToken);

            return ids.ToHashSet();
        }

        return teacherSubject.TeacherSubjectUnits
            .Select(u => u.UnitId)
            .ToHashSet();
    }

    public async Task<string?> ValidateSessionUnitsInRepertoireAsync(
        TeacherSubject teacherSubject,
        IReadOnlyList<CreateCourseSessionDto>? sessions,
        CancellationToken cancellationToken = default)
    {
        if (sessions == null) return null;

        var allowedUnitIds = await GetAllowedUnitIdsAsync(teacherSubject, cancellationToken);

        for (var i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            if (session.Units == null) continue;

            var error = await ValidateUnitRowsAsync(
                allowedUnitIds,
                session.Units,
                $"Session {i + 1}",
                cancellationToken);

            if (error != null) return error;
        }

        return null;
    }

    public async Task<string?> ValidateUnitRowsInRepertoireAsync(
        TeacherSubject teacherSubject,
        IReadOnlyList<CreateCourseSessionUnitDto> units,
        CancellationToken cancellationToken = default)
    {
        var allowedUnitIds = await GetAllowedUnitIdsAsync(teacherSubject, cancellationToken);
        return await ValidateUnitRowsAsync(allowedUnitIds, units, "Session", cancellationToken);
    }

    private async Task<List<TeacherSubjectUnitOptionDto>> MapAllowedUnitsAsync(
        TeacherSubject teacherSubject,
        CancellationToken cancellationToken)
    {
        if (teacherSubject.CanTeachFullSubject)
        {
            return await _contentUnitRepository
                .GetContentUnitsBySubjectId(teacherSubject.SubjectId)
                .Where(cu => cu.IsActive)
                .OrderBy(cu => cu.OrderIndex)
                .Select(cu => new TeacherSubjectUnitOptionDto
                {
                    Id = cu.Id,
                    NameAr = cu.NameAr,
                    NameEn = cu.NameEn,
                })
                .ToListAsync(cancellationToken);
        }

        return teacherSubject.TeacherSubjectUnits
            .GroupBy(u => u.UnitId)
            .Select(g =>
            {
                var first = g.First();
                return new TeacherSubjectUnitOptionDto
                {
                    Id = first.UnitId,
                    NameAr = first.Unit?.NameAr ?? string.Empty,
                    NameEn = first.Unit?.NameEn ?? string.Empty,
                    QuranContentTypeId = first.QuranContentTypeId,
                    QuranLevelId = first.QuranLevelId,
                };
            })
            .OrderBy(u => u.NameAr)
            .ThenBy(u => u.NameEn)
            .ToList();
    }

    private async Task<string?> ValidateUnitRowsAsync(
        HashSet<int> allowedUnitIds,
        IReadOnlyList<CreateCourseSessionUnitDto> units,
        string label,
        CancellationToken cancellationToken)
    {
        foreach (var unit in units)
        {
            if (unit.ContentUnitId.HasValue)
            {
                if (!allowedUnitIds.Contains(unit.ContentUnitId.Value))
                {
                    return $"{label}: contentUnitId {unit.ContentUnitId} is outside this teacher's repertoire.";
                }

                continue;
            }

            if (!unit.LessonId.HasValue) continue;

            var lesson = await _lessonRepository.GetByIdAsync(unit.LessonId.Value);
            if (lesson == null || !allowedUnitIds.Contains(lesson.UnitId))
            {
                return $"{label}: lessonId {unit.LessonId} is outside this teacher's repertoire.";
            }
        }

        return null;
    }
}
