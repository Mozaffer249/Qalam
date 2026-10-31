using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.LRS;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations.LRS;

public class LrsDispatchService : ILrsDispatchService
{
    private readonly LrsSettings _settings;
    private readonly IXApiStatementBuilder _builder;
    private readonly IRabbitMQService _rabbitMQ;
    private readonly ApplicationDBContext _db;
    private readonly ILogger<LrsDispatchService> _logger;

    public LrsDispatchService(
        IOptions<LrsSettings> settings,
        IXApiStatementBuilder builder,
        IRabbitMQService rabbitMQ,
        ApplicationDBContext db,
        ILogger<LrsDispatchService> logger)
    {
        _settings = settings.Value;
        _builder = builder;
        _rabbitMQ = rabbitMQ;
        _db = db;
        _logger = logger;
    }

    private async Task<bool> IsEnabledAsync(CancellationToken ct)
    {
        if (_settings.Enabled) return true;
        try
        {
            var row = await _db.Set<Data.Entity.Common.SystemSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == "LrsCredentials", ct);
            if (row != null && !string.IsNullOrEmpty(row.Value))
            {
                var doc = JsonSerializer.Deserialize<JsonElement>(row.Value);
                if (doc.TryGetProperty("enabled", out var e) && e.GetBoolean())
                    return true;
            }
        }
        catch { /* fall through */ }
        return false;
    }

    public async Task DispatchSessionCompletedAsync(CourseSchedule schedule, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct)) return;

        try
        {
            var enrollment = schedule.Enrollment
                ?? await _db.Enrollments
                    .Include(e => e.Course)
                    .Include(e => e.Participants).ThenInclude(p => p.Student)
                    .Include(e => e.ApprovedByTeacher)
                    .FirstOrDefaultAsync(e => e.Id == schedule.EnrollmentId, ct);

            if (enrollment?.Course == null) return;

            var teacherUser = await ResolveTeacherUserAsync(enrollment, ct);

            foreach (var participant in enrollment.Participants)
            {
                var (studentUser, identityNumber) = await ResolveStudentUserAsync(participant.StudentId, ct);
                if (studentUser == null) continue;

                var attendance = schedule.Attendances?.FirstOrDefault(a => a.StudentId == participant.StudentId);
                var stmt = _builder.BuildSessionCompleted(studentUser, schedule, enrollment.Course, teacherUser, attendance?.Status, identityNumber);
                await QueueAsync(stmt, "completed", studentUser, $"sessions/{schedule.Id}", "CourseSchedule", schedule.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue LRS session-completed for CourseSchedule {Id}", schedule.Id);
        }
    }

    public async Task DispatchSessionLaunchedAsync(CourseSchedule schedule, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct)) return;

        try
        {
            var enrollment = schedule.Enrollment
                ?? await _db.Enrollments
                    .Include(e => e.Course)
                    .Include(e => e.Participants)
                    .FirstOrDefaultAsync(e => e.Id == schedule.EnrollmentId, ct);

            if (enrollment?.Course == null) return;

            foreach (var participant in enrollment.Participants)
            {
                var (studentUser, identityNumber) = await ResolveStudentUserAsync(participant.StudentId, ct);
                if (studentUser == null) continue;

                var stmt = _builder.BuildSessionLaunched(studentUser, schedule, enrollment.Course, identityNumber);
                await QueueAsync(stmt, "launched", studentUser, $"sessions/{schedule.Id}", "CourseSchedule", schedule.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue LRS session-launched for CourseSchedule {Id}", schedule.Id);
        }
    }

    public async Task DispatchStudentAttendedAsync(int studentId, CourseSchedule schedule, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct)) return;

        try
        {
            var course = schedule.Enrollment?.Course
                ?? await _db.Courses.FindAsync(new object[] { schedule.Enrollment?.CourseId ?? 0 }, ct);

            if (course == null) return;

            var (studentUser, identityNumber) = await ResolveStudentUserAsync(studentId, ct);
            if (studentUser == null) return;

            var stmt = _builder.BuildSessionAttended(studentUser, schedule, course, identityNumber);
            await QueueAsync(stmt, "attended", studentUser, $"sessions/{schedule.Id}", "CourseSchedule", schedule.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue LRS student-attended for Student {StudentId}, CourseSchedule {ScheduleId}", studentId, schedule.Id);
        }
    }

    public async Task DispatchEnrollmentActivatedAsync(Enrollment enrollment, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct)) return;

        try
        {
            var course = enrollment.Course
                ?? await _db.Courses.FindAsync(new object[] { enrollment.CourseId ?? 0 }, ct);

            if (course == null) return;

            var teacherUser = await ResolveTeacherUserAsync(enrollment, ct);

            foreach (var participant in enrollment.Participants)
            {
                var (studentUser, identityNumber) = await ResolveStudentUserAsync(participant.StudentId, ct);
                if (studentUser == null) continue;

                var stmt = _builder.BuildRegistered(studentUser, course, enrollment, teacherUser, identityNumber);
                await QueueAsync(stmt, "registered", studentUser, $"courses/{course.Id}", "Enrollment", enrollment.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue LRS enrollment-activated for Enrollment {Id}", enrollment.Id);
        }
    }

    public async Task DispatchProgressUpdatedAsync(CourseSchedule schedule, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct)) return;

        try
        {
            var enrollment = schedule.Enrollment
                ?? await _db.Enrollments
                    .Include(e => e.Course)
                    .Include(e => e.Participants)
                    .Include(e => e.CourseSchedules)
                    .FirstOrDefaultAsync(e => e.Id == schedule.EnrollmentId, ct);

            if (enrollment?.Course == null) return;

            var totalSessions = enrollment.CourseSchedules.Count;
            var completedSessions = enrollment.CourseSchedules
                .Count(s => s.Status == ScheduleStatus.Completed);

            foreach (var participant in enrollment.Participants)
            {
                var (studentUser, identityNumber) = await ResolveStudentUserAsync(participant.StudentId, ct);
                if (studentUser == null) continue;

                var stmt = _builder.BuildProgressUpdated(studentUser, enrollment.Course, enrollment, completedSessions, totalSessions, identityNumber);
                await QueueAsync(stmt, "progressed", studentUser, $"courses/{enrollment.Course.Id}", "Enrollment", enrollment.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue LRS progress-updated for CourseSchedule {Id}", schedule.Id);
        }
    }

    public async Task DispatchCourseCompletedAsync(Enrollment enrollment, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct)) return;

        try
        {
            var course = enrollment.Course
                ?? await _db.Courses.FindAsync(new object[] { enrollment.CourseId ?? 0 }, ct);

            if (course == null) return;

            foreach (var participant in enrollment.Participants)
            {
                var (studentUser, identityNumber) = await ResolveStudentUserAsync(participant.StudentId, ct);
                if (studentUser == null) continue;

                var stmt = _builder.BuildCourseCompleted(studentUser, course, enrollment, identityNumber);
                await QueueAsync(stmt, "completed", studentUser, $"courses/{course.Id}", "Enrollment", enrollment.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue LRS course-completed for Enrollment {Id}", enrollment.Id);
        }
    }

    private async Task QueueAsync(Data.DTOs.LRS.XApiStatement stmt, string verb, User student, string objectId, string sourceType, int sourceId)
    {
        var statementJson = JsonSerializer.Serialize(stmt);
        var actorIdentifier = !string.IsNullOrEmpty(student.Email)
            ? student.Email
            : $"user-{student.Id}";

        var log = new LrsStatementLog
        {
            StatementId = Guid.TryParse(stmt.Id, out var parsedId) ? parsedId : Guid.NewGuid(),
            Verb = verb,
            ActorIdentifier = actorIdentifier,
            ObjectIri = objectId,
            Status = LrsStatementStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            SourceEntityType = sourceType,
            SourceEntityId = sourceId,
            StatementJson = statementJson,
        };
        _db.LrsStatementLogs.Add(log);
        await _db.SaveChangesAsync();

        var msg = new LrsStatementMessage
        {
            LogId = log.Id,
            StatementJson = statementJson,
            Verb = verb,
            ActorName = $"{student.FirstName} {student.LastName}".Trim(),
            ObjectId = objectId,
            Timestamp = DateTime.UtcNow,
            SourceEntityType = sourceType,
            SourceEntityId = sourceId,
            QueuedAt = DateTime.UtcNow
        };
        await _rabbitMQ.QueueLrsStatementAsync(msg);
    }

    public async Task<int> ReplayFullJourneyAsync(int enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.Course)
            .Include(e => e.Participants).ThenInclude(p => p.Student)
            .Include(e => e.CourseSchedules).ThenInclude(s => s.Attendances)
            .Include(e => e.ApprovedByTeacher)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);

        if (enrollment?.Course == null) return 0;

        var count = 0;
        var course = enrollment.Course;
        var teacherUser = await ResolveTeacherUserAsync(enrollment, ct);
        var completedSchedules = enrollment.CourseSchedules
            .Where(s => s.Status == ScheduleStatus.Completed)
            .OrderBy(s => s.Date).ThenBy(s => s.Id)
            .ToList();
        var totalSessions = enrollment.CourseSchedules.Count;

        foreach (var participant in enrollment.Participants)
        {
            var (studentUser, identityNumber) = await ResolveStudentUserAsync(participant.StudentId, ct);
            if (studentUser == null) continue;

            // 1. registered
            var reg = _builder.BuildRegistered(studentUser, course, enrollment, teacherUser, identityNumber);
            await QueueAsync(reg, "registered", studentUser, $"courses/{course.Id}", "Enrollment", enrollment.Id);
            count++;

            // 2-5. Per completed session: initialized → attended → completed → progressed
            var completedSoFar = 0;
            foreach (var schedule in completedSchedules)
            {
                var init = _builder.BuildSessionLaunched(studentUser, schedule, course, identityNumber);
                await QueueAsync(init, "launched", studentUser, $"sessions/{schedule.Id}", "CourseSchedule", schedule.Id);
                count++;

                var att = _builder.BuildSessionAttended(studentUser, schedule, course, identityNumber);
                await QueueAsync(att, "attended", studentUser, $"sessions/{schedule.Id}", "CourseSchedule", schedule.Id);
                count++;

                var attendance = schedule.Attendances?.FirstOrDefault(a => a.StudentId == participant.StudentId);
                var comp = _builder.BuildSessionCompleted(studentUser, schedule, course, teacherUser, attendance?.Status, identityNumber);
                await QueueAsync(comp, "completed", studentUser, $"sessions/{schedule.Id}", "CourseSchedule", schedule.Id);
                count++;

                completedSoFar++;
                var prog = _builder.BuildProgressUpdated(studentUser, course, enrollment, completedSoFar, totalSessions, identityNumber);
                await QueueAsync(prog, "progressed", studentUser, $"courses/{course.Id}", "Enrollment", enrollment.Id);
                count++;
            }

            // 6. course completed (if enrollment is completed)
            if (enrollment.EnrollmentStatus == EnrollmentStatus.Completed)
            {
                var cc = _builder.BuildCourseCompleted(studentUser, course, enrollment, identityNumber);
                await QueueAsync(cc, "completed", studentUser, $"courses/{course.Id}", "Enrollment", enrollment.Id);
                count++;
            }
        }

        return count;
    }

    private async Task<(User? User, string? IdentityNumber)> ResolveStudentUserAsync(int studentId, CancellationToken ct)
    {
        var student = await _db.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);
        return (student?.User, student?.IdentityNumber);
    }

    private async Task<User?> ResolveTeacherUserAsync(Enrollment enrollment, CancellationToken ct)
    {
        var teacher = await _db.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == enrollment.ApprovedByTeacherId, ct);
        return teacher?.User;
    }
}
