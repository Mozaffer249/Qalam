using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

/// <summary>
/// Hybrid progression: evaluates metrics and creates pending upgrade suggestions for admin review.
/// </summary>
public class TeacherLevelProgressionService : ITeacherLevelProgressionService
{
    private const decimal MinRating = 4.5m;
    private const int MinCompletedSessions = 50;
    private const decimal MinAttendanceRate = 95m;

    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherLevelRepository _levelRepository;
    private readonly ITeacherLevelUpgradeSuggestionRepository _suggestionRepository;
    private readonly ApplicationDBContext _db;

    public TeacherLevelProgressionService(
        ITeacherRepository teacherRepository,
        ITeacherLevelRepository levelRepository,
        ITeacherLevelUpgradeSuggestionRepository suggestionRepository,
        ApplicationDBContext db)
    {
        _teacherRepository = teacherRepository;
        _levelRepository = levelRepository;
        _suggestionRepository = suggestionRepository;
        _db = db;
    }

    public async Task EvaluateTeacherAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByIdWithLevelAsync(teacherId, cancellationToken);
        if (teacher?.TeacherLevel == null)
            return;

        var nextLevel = await _levelRepository.GetNextLevelAsync(teacher.TeacherLevel.OrderIndex, cancellationToken);
        if (nextLevel == null)
            return;

        var existingPending = await _suggestionRepository.GetPendingForTeacherAsync(teacherId, cancellationToken);
        if (existingPending != null)
            return;

        var metrics = await LoadMetricsAsync(teacherId, cancellationToken);
        if (metrics.CompletedSessions < MinCompletedSessions
            || metrics.AvgRating < MinRating
            || metrics.AttendanceRate < MinAttendanceRate)
        {
            return;
        }

        var suggestion = new TeacherLevelUpgradeSuggestion
        {
            TeacherId = teacherId,
            CurrentLevelId = teacher.TeacherLevelId!.Value,
            SuggestedLevelId = nextLevel.Id,
            AvgRating = metrics.AvgRating,
            CompletedSessions = metrics.CompletedSessions,
            AttendanceRate = metrics.AttendanceRate,
            Status = TeacherLevelUpgradeSuggestionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _suggestionRepository.AddAsync(suggestion);
        await _suggestionRepository.SaveChangesAsync();
    }

    private async Task<(decimal AvgRating, int CompletedSessions, decimal AttendanceRate)> LoadMetricsAsync(
        int teacherId,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        var avgRating = teacher?.RatingAverage ?? 0m;

        var completedSessions = await _db.CourseSchedules
            .AsNoTracking()
            .CountAsync(
                s => s.Enrollment.ApprovedByTeacherId == teacherId && s.Status == ScheduleStatus.Completed,
                cancellationToken);

        var attendanceRows = await _db.SessionAttendances
            .AsNoTracking()
            .Where(a => a.CourseSchedule.Enrollment.ApprovedByTeacherId == teacherId)
            .Select(a => a.Status)
            .ToListAsync(cancellationToken);

        var attendanceRate = attendanceRows.Count == 0
            ? 100m
            : Math.Round(
                attendanceRows.Count(s => s is SessionAttendanceStatus.Present or SessionAttendanceStatus.Late) * 100m
                / attendanceRows.Count,
                2);

        return (avgRating, completedSessions, attendanceRate);
    }
}
