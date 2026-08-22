using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

/// <summary>
/// Hybrid progression: evaluates per-domain metrics and creates pending upgrade suggestions for admin review.
/// </summary>
public class TeacherLevelProgressionService : ITeacherLevelProgressionService
{
    private const decimal MinRating = 4.5m;
    private const int MinCompletedSessions = 50;
    private const decimal MinAttendanceRate = 95m;

    private readonly ITeacherDomainPricingRepository _domainPricingRepository;
    private readonly ITeacherLevelRepository _levelRepository;
    private readonly ITeacherLevelUpgradeSuggestionRepository _suggestionRepository;
    private readonly ApplicationDBContext _db;

    public TeacherLevelProgressionService(
        ITeacherDomainPricingRepository domainPricingRepository,
        ITeacherLevelRepository levelRepository,
        ITeacherLevelUpgradeSuggestionRepository suggestionRepository,
        ApplicationDBContext db)
    {
        _domainPricingRepository = domainPricingRepository;
        _levelRepository = levelRepository;
        _suggestionRepository = suggestionRepository;
        _db = db;
    }

    public async Task EvaluateTeacherAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default)
    {
        if (domainId <= 0)
            return;

        var pricing = await _domainPricingRepository.GetByTeacherAndDomainAsync(
            teacherId, domainId, cancellationToken);
        if (pricing?.TeacherLevel == null || !pricing.HasCompletedInterviewSession)
            return;

        var nextLevel = await _levelRepository.GetNextLevelAsync(pricing.TeacherLevel.OrderIndex, cancellationToken);
        if (nextLevel == null)
            return;

        var existingPending = await _suggestionRepository.GetPendingForTeacherDomainAsync(
            teacherId, domainId, cancellationToken);
        if (existingPending != null)
            return;

        var metrics = await LoadMetricsAsync(teacherId, domainId, cancellationToken);
        if (metrics.CompletedSessions < MinCompletedSessions
            || metrics.AvgRating < MinRating
            || metrics.AttendanceRate < MinAttendanceRate)
        {
            return;
        }

        var suggestion = new TeacherLevelUpgradeSuggestion
        {
            TeacherId = teacherId,
            DomainId = domainId,
            CurrentLevelId = pricing.TeacherLevelId!.Value,
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
        int domainId,
        CancellationToken cancellationToken)
    {
        var avgRating = await _db.Teachers.AsNoTracking()
            .Where(t => t.Id == teacherId)
            .Select(t => t.RatingAverage)
            .FirstOrDefaultAsync(cancellationToken);

        var completedSessions = await _db.CourseSchedules
            .AsNoTracking()
            .CountAsync(
                s => (s.Enrollment.ApprovedByTeacherId == teacherId
                      || s.Enrollment.Course.TeacherId == teacherId)
                     && s.Enrollment.Course.TeacherSubject.Subject.DomainId == domainId
                     && s.Status == ScheduleStatus.Completed,
                cancellationToken);

        var attendanceRows = await _db.SessionAttendances
            .AsNoTracking()
            .Where(a => (a.CourseSchedule.Enrollment.ApprovedByTeacherId == teacherId
                         || a.CourseSchedule.Enrollment.Course.TeacherId == teacherId)
                        && a.CourseSchedule.Enrollment.Course.TeacherSubject.Subject.DomainId == domainId)
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
