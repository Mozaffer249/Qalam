using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TargetedOpenSessionRequestValidator : ITargetedOpenSessionRequestValidator
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly ITeacherSubjectRepository _teacherSubjectRepo;
    private readonly ILessonRepository _lessonRepo;

    public TargetedOpenSessionRequestValidator(
        ITeacherRepository teacherRepo,
        ITeacherSubjectRepository teacherSubjectRepo,
        ILessonRepository lessonRepo)
    {
        _teacherRepo = teacherRepo;
        _teacherSubjectRepo = teacherSubjectRepo;
        _lessonRepo = lessonRepo;
    }

    public async Task<string?> ValidateAsync(
        int targetedTeacherId,
        int subjectId,
        IReadOnlyList<CreateOpenSessionRequestSessionDto> sessions,
        CancellationToken cancellationToken = default)
    {
        // 1. Teacher exists and is active
        var teacher = await _teacherRepo.GetByIdAsync(targetedTeacherId);
        if (teacher is null || !teacher.IsActive)
            return "المعلم المستهدف غير موجود أو غير نشط.";

        // 2. Teacher offers the requested subject (active TeacherSubject row) + load its units
        var teacherSubjects = await _teacherSubjectRepo.GetTeacherSubjectsWithUnitsAsync(targetedTeacherId);
        var match = teacherSubjects.FirstOrDefault(ts => ts.SubjectId == subjectId && ts.IsActive);
        if (match is null)
            return "هذا المعلم لا يُدرّس المادة المطلوبة. اختر معلماً آخر أو غيّر المادة.";

        var allowedUnitIds = match.TeacherSubjectUnits.Select(u => u.UnitId).ToHashSet();

        // 3. Per-session, per-row validation
        for (var i = 0; i < sessions.Count; i++)
        {
            var s = sessions[i];
            var label = $"Session {s.SequenceNumber}";

            foreach (var u in s.Units)
            {
                var hasContentUnit = u.ContentUnitId.HasValue;
                var hasLesson = u.LessonId.HasValue;

                if (hasContentUnit == hasLesson)
                    return $"{label}: each unit row must set exactly one of contentUnitId or lessonId.";

                if (hasLesson && u.IncludesAllLessons)
                    return $"{label}: includesAllLessons must be false when lessonId is set — single-lesson rows can't expand.";

                if (hasContentUnit)
                {
                    if (!allowedUnitIds.Contains(u.ContentUnitId!.Value))
                        return $"{label}: contentUnitId {u.ContentUnitId} is outside this teacher's repertoire.";
                }
                else // hasLesson
                {
                    var lesson = await _lessonRepo.GetByIdAsync(u.LessonId!.Value);
                    if (lesson is null || !allowedUnitIds.Contains(lesson.UnitId))
                        return $"{label}: lessonId {u.LessonId} is outside this teacher's repertoire.";
                }
            }
        }

        return null;
    }
}
