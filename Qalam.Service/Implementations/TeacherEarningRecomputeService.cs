using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherEarningRecomputeService : ITeacherEarningRecomputeService
{
    private readonly ApplicationDBContext _db;
    private readonly ITeacherLevelRepository _teacherLevelRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<TeacherEarningRecomputeService> _logger;

    public TeacherEarningRecomputeService(
        ApplicationDBContext db,
        ITeacherLevelRepository teacherLevelRepository,
        IAuditService auditService,
        ILogger<TeacherEarningRecomputeService> logger)
    {
        _db = db;
        _teacherLevelRepository = teacherLevelRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<AdminTeacherEarningsRecomputeResultDto> RecomputeAsync(
        bool dryRun,
        int? adminUserId,
        CancellationToken cancellationToken = default)
    {
        var result = new AdminTeacherEarningsRecomputeResultDto { DryRun = dryRun };

        var enrollments = await _db.Enrollments
            .Include(e => e.PricingSnapshot)
            .Include(e => e.CourseSchedules)
            .Where(e => e.PricingSnapshot != null && e.EnrollmentStatus != EnrollmentStatus.Cancelled)
            .ToListAsync(cancellationToken);
        result.EnrollmentsScanned = enrollments.Count;
        if (enrollments.Count == 0)
            return result;

        var enrollmentIds = enrollments.Select(e => e.Id).ToList();
        var linesByEnrollment = (await _db.TeacherEarningLines
                .Where(l => enrollmentIds.Contains(l.EnrollmentId) && l.CourseScheduleId != null)
                .ToListAsync(cancellationToken))
            .GroupBy(l => l.EnrollmentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var teacherIds = enrollments
            .Select(e => ResolveTeacherId(e))
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var interviewScheduleByTeacher = await _db.Teachers
            .AsNoTracking()
            .Where(t => teacherIds.Contains(t.Id))
            .Select(t => new { t.Id, t.InterviewUnlockCourseScheduleId })
            .ToDictionaryAsync(t => t.Id, t => t.InterviewUnlockCourseScheduleId, cancellationToken);
        var domainPricings = await _db.TeacherDomainPricings
            .AsNoTracking()
            .Include(p => p.TeacherLevel)
            .Where(p => teacherIds.Contains(p.TeacherId))
            .ToListAsync(cancellationToken);
        var starter = await _teacherLevelRepository.GetStarterLevelAsync(cancellationToken);

        foreach (var enrollment in enrollments)
        {
            var snap = enrollment.PricingSnapshot!;
            var teacherId = ResolveTeacherId(enrollment);
            if (teacherId <= 0)
                continue;

            interviewScheduleByTeacher.TryGetValue(teacherId, out var interviewScheduleId);
            var lines = linesByEnrollment.GetValueOrDefault(enrollment.Id) ?? [];
            var schedules = (enrollment.CourseSchedules ?? [])
                .Where(s => s.Status != ScheduleStatus.Cancelled && s.Status != ScheduleStatus.Rescheduled)
                .ToList();

            var settled = schedules.Count > 0 && schedules.All(s =>
                s.Id == interviewScheduleId
                || lines.Any(l => l.CourseScheduleId == s.Id
                                  && l.Status is TeacherEarningLineStatus.IncludedInPayout or TeacherEarningLineStatus.Voided));
            if (settled)
                continue;

            var newShare = snap.TeacherSharePct;
            int? newLevelId = snap.TeacherLevelId;
            if (newShare <= 0m)
            {
                var pricing = domainPricings.FirstOrDefault(p => p.TeacherId == teacherId && p.DomainId == snap.DomainId);
                if (pricing?.CustomTeacherSharePct is > 0)
                    newShare = pricing.CustomTeacherSharePct.Value;
                else if (pricing?.TeacherLevel != null)
                {
                    newShare = pricing.TeacherLevel.TeacherSharePct;
                    newLevelId = pricing.TeacherLevelId;
                }
                else if (starter != null)
                {
                    newShare = starter.TeacherSharePct;
                    newLevelId = starter.Id;
                }
            }

            var earningsHourly = snap.EarningsPricePerHour ?? snap.PricePerHour;
            var earningsBase = Math.Round(snap.TotalMinutes / 60m * earningsHourly, 2, MidpointRounding.AwayFromZero);
            var newEarnings = Math.Round(earningsBase * newShare / 100m, 2, MidpointRounding.AwayFromZero);
            var newPlatform = Math.Round(snap.TotalPrice - newEarnings, 2, MidpointRounding.AwayFromZero);

            var item = new AdminTeacherEarningsRecomputeItemDto
            {
                EnrollmentId = enrollment.Id,
                TeacherId = teacherId,
                OldTeacherSharePct = snap.TeacherSharePct,
                NewTeacherSharePct = newShare,
                OldTeacherEarnings = snap.TeacherEarnings,
                NewTeacherEarnings = newEarnings,
                OldPlatformShare = snap.PlatformShare,
                NewPlatformShare = newPlatform,
            };
            var snapshotChanged = newShare != snap.TeacherSharePct
                                  || newEarnings != snap.TeacherEarnings
                                  || newPlatform != snap.PlatformShare;

            var totalMinutes = snap.TotalMinutes > 0 ? snap.TotalMinutes : schedules.Sum(s => s.DurationMinutes);
            foreach (var schedule in schedules.Where(s => s.Status == ScheduleStatus.Completed && s.Id != interviewScheduleId))
            {
                var amount = TeacherEarningService.ComputeScheduleEarning(newEarnings, totalMinutes, schedule.DurationMinutes);
                var scheduleLines = lines.Where(l => l.CourseScheduleId == schedule.Id).ToList();
                if (scheduleLines.Count == 0)
                {
                    if (amount <= 0m)
                        continue;
                    item.LinesCreated++;
                    item.LineAmountDelta += amount;
                    if (!dryRun)
                    {
                        _db.TeacherEarningLines.Add(new TeacherEarningLine
                        {
                            TeacherId = teacherId,
                            EnrollmentId = enrollment.Id,
                            CourseScheduleId = schedule.Id,
                            Amount = amount,
                            Currency = snap.Currency,
                            Source = TeacherEarningSource.SessionCompleted,
                            Status = TeacherEarningLineStatus.Pending,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    continue;
                }

                foreach (var line in scheduleLines.Where(l =>
                             l.Status is TeacherEarningLineStatus.Pending or TeacherEarningLineStatus.OnHold
                             && l.Amount != amount))
                {
                    item.LinesUpdated++;
                    item.LineAmountDelta += amount - line.Amount;
                    if (!dryRun)
                        line.Amount = amount;
                }
            }

            if (!snapshotChanged && item.LinesCreated == 0 && item.LinesUpdated == 0)
                continue;

            if (snapshotChanged)
            {
                result.SnapshotsUpdated++;
                result.SnapshotEarningsDelta += newEarnings - snap.TeacherEarnings;
                if (!dryRun)
                {
                    snap.TeacherSharePct = newShare;
                    snap.TeacherLevelId = newLevelId;
                    snap.TeacherEarnings = newEarnings;
                    snap.PlatformShare = newPlatform;
                    snap.UpdatedAt = DateTime.UtcNow;
                }
            }

            result.LinesCreated += item.LinesCreated;
            result.LinesUpdated += item.LinesUpdated;
            result.LineAmountDelta += item.LineAmountDelta;
            result.Changes.Add(item);
        }

        if (!dryRun)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Recomputed teacher earnings: {Snapshots} snapshots, {Created} lines created, {Updated} lines updated.",
                result.SnapshotsUpdated, result.LinesCreated, result.LinesUpdated);
            await _auditService.LogAsync(
                "RecomputeTeacherEarnings",
                adminUserId,
                "system",
                true,
                details: JsonSerializer.Serialize(new
                {
                    result.EnrollmentsScanned,
                    result.SnapshotsUpdated,
                    result.LinesCreated,
                    result.LinesUpdated,
                    result.SnapshotEarningsDelta,
                    result.LineAmountDelta,
                    EnrollmentIds = result.Changes.Select(c => c.EnrollmentId).ToList()
                }),
                entityType: "TeacherEarnings");
        }

        return result;
    }

    private static int ResolveTeacherId(Data.Entity.Course.Enrollment enrollment)
    {
        if (enrollment.PricingSnapshot?.TeacherId > 0)
            return enrollment.PricingSnapshot.TeacherId;
        return enrollment.ApprovedByTeacherId;
    }
}
