using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.DTOs.LRS;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.LRS;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations.LRS.Building;

namespace Qalam.Service.Implementations.LRS;

public class LrsDispatchService : ILrsDispatchService
{
    private readonly IXApiStatementBuilder _builder;
    private readonly IRabbitMQService _rabbitMQ;
    private readonly ApplicationDBContext _db;
    private readonly ILogger<LrsDispatchService> _logger;

    public LrsDispatchService(
        IXApiStatementBuilder builder,
        IRabbitMQService rabbitMQ,
        ApplicationDBContext db,
        ILogger<LrsDispatchService> logger)
    {
        _builder = builder;
        _rabbitMQ = rabbitMQ;
        _db = db;
        _logger = logger;
    }

    private async Task<bool> IsEnabledAsync(CancellationToken ct)
    {
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

    public async Task DispatchEnrollmentActivatedAsync(Enrollment enrollment, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct)) return;
        await _builder.LoadSettingsAsync(ct);

        try
        {
            var loaded = await LoadEnrollmentAsync(enrollment.Id, ct);
            if (loaded?.Course == null) return;

            var course = loaded.Course;
            var teacher = await ResolveTeacherUserAsync(loaded, ct);
            var courseObject = $"courses/{course.Id}";

            await ForEachParticipantAsync(loaded,
                (student, id) => _builder.BuildRegistered(student, course, loaded, teacher, id),
                "registered", courseObject, loaded.Id, ct);

            await ForEachParticipantAsync(loaded,
                (student, id) => _builder.BuildInitialized(student, course, teacher, id),
                "initialized", courseObject, loaded.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue LRS enrollment-activated for Enrollment {Id}", enrollment.Id);
        }
    }

    public async Task DispatchProgressUpdatedAsync(CourseSchedule schedule, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct)) return;
        await _builder.LoadSettingsAsync(ct);

        try
        {
            var enrollment = schedule.Enrollment
                ?? await _db.Enrollments
                    .Include(e => e.Course)
                    .Include(e => e.Participants)
                    .Include(e => e.CourseSchedules)
                    .FirstOrDefaultAsync(e => e.Id == schedule.EnrollmentId, ct);

            if (enrollment?.Course == null) return;

            var course = enrollment.Course;
            var totalSessions = enrollment.CourseSchedules.Count;
            var completedSessions = enrollment.CourseSchedules.Count(s => s.Status == ScheduleStatus.Completed);

            await ForEachParticipantAsync(enrollment,
                (student, id) => _builder.BuildProgressUpdated(student, course, enrollment, completedSessions, totalSessions, id),
                "progressed", $"courses/{course.Id}", enrollment.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue LRS progress-updated for CourseSchedule {Id}", schedule.Id);
        }
    }

    public async Task DispatchCourseCompletedAsync(Enrollment enrollment, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct)) return;
        await _builder.LoadSettingsAsync(ct);

        try
        {
            var loaded = await LoadEnrollmentAsync(enrollment.Id, ct);
            if (loaded?.Course == null) return;

            var course = loaded.Course;

            await ForEachParticipantAsync(loaded,
                (student, id) => _builder.BuildCourseCompleted(student, course, loaded, id),
                "completed", $"courses/{course.Id}", loaded.Id, ct);

            await ForEachParticipantAsync(loaded,
                (student, id) => _builder.BuildCourseEarned(student, course, loaded, id),
                "earned", $"courses/{course.Id}/certificate", loaded.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue LRS course-completed for Enrollment {Id}", enrollment.Id);
        }
    }

    public async Task DispatchStudentRatedAsync(int studentId, int courseScheduleId, int rating, string? feedback, CancellationToken ct = default)
    {
        // NELC requires a text response on rated; a comment-less rating is simply not reported.
        if (string.IsNullOrWhiteSpace(feedback)) return;
        if (!await IsEnabledAsync(ct)) return;
        await _builder.LoadSettingsAsync(ct);

        try
        {
            var schedule = await _db.Set<CourseSchedule>()
                .Include(cs => cs.Enrollment).ThenInclude(e => e!.Course)
                .FirstOrDefaultAsync(cs => cs.Id == courseScheduleId, ct);

            var course = schedule?.Enrollment?.Course;
            if (course == null) return;

            var (studentUser, identityNumber) = await ResolveStudentUserAsync(studentId, ct);
            if (studentUser == null) return;

            var stmt = _builder.BuildRated(studentUser, course, rating, feedback, identityNumber);
            await QueueAsync(stmt, "rated", studentUser, $"courses/{course.Id}", "TeacherReview", courseScheduleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue LRS rated for Student {StudentId}, CourseSchedule {ScheduleId}", studentId, courseScheduleId);
        }
    }

    public async Task<int> ReplayFullJourneyAsync(int enrollmentId, CancellationToken ct = default)
    {
        await _builder.LoadSettingsAsync(ct);

        var enrollment = await _db.Enrollments
            .Include(e => e.Course)
            .Include(e => e.Participants).ThenInclude(p => p.Student)
            .Include(e => e.CourseSchedules)
            .Include(e => e.ApprovedByTeacher)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);

        if (enrollment?.Course == null) return 0;

        var course = enrollment.Course;
        var teacher = await ResolveTeacherUserAsync(enrollment, ct);
        var scheduleIds = enrollment.CourseSchedules.Select(cs => cs.Id).ToList();
        var totalSessions = enrollment.CourseSchedules.Count;
        var completedSessions = enrollment.CourseSchedules.Count(s => s.Status == ScheduleStatus.Completed);
        var isCompleted = enrollment.EnrollmentStatus == EnrollmentStatus.Completed;
        var courseObject = $"courses/{course.Id}";
        var count = 0;

        foreach (var participant in enrollment.Participants)
        {
            var (student, identityNumber) = await ResolveStudentUserAsync(participant.StudentId, ct);
            if (student == null) continue;

            await QueueAsync(_builder.BuildRegistered(student, course, enrollment, teacher, identityNumber),
                "registered", student, courseObject, "Enrollment", enrollment.Id);
            count++;

            await QueueAsync(_builder.BuildInitialized(student, course, teacher, identityNumber),
                "initialized", student, courseObject, "Enrollment", enrollment.Id);
            count++;

            if (completedSessions > 0)
            {
                await QueueAsync(_builder.BuildProgressUpdated(student, course, enrollment, completedSessions, totalSessions, identityNumber),
                    "progressed", student, courseObject, "Enrollment", enrollment.Id);
                count++;
            }

            if (isCompleted)
            {
                await QueueAsync(_builder.BuildCourseCompleted(student, course, enrollment, identityNumber),
                    "completed", student, courseObject, "Enrollment", enrollment.Id);
                count++;

                await QueueAsync(_builder.BuildCourseEarned(student, course, enrollment, identityNumber),
                    "earned", student, $"courses/{course.Id}/certificate", "Enrollment", enrollment.Id);
                count++;
            }

            var review = await _db.Set<TeacherReview>()
                .Where(r => r.StudentId == participant.StudentId && r.SessionId != null && scheduleIds.Contains(r.SessionId.Value)
                    && r.Feedback != null && r.Feedback != "")
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync(ct);

            if (review != null)
            {
                await QueueAsync(_builder.BuildRated(student, course, review.Rating, review.Feedback, identityNumber),
                    "rated", student, courseObject, "TeacherReview", review.Id);
                count++;
            }
        }

        return count;
    }

    private async Task ForEachParticipantAsync(
        Enrollment enrollment,
        Func<User, string?, XApiStatement> build,
        string verb,
        string objectId,
        int sourceId,
        CancellationToken ct)
    {
        foreach (var participant in enrollment.Participants)
        {
            var (student, identityNumber) = await ResolveStudentUserAsync(participant.StudentId, ct);
            if (student == null) continue;

            await QueueAsync(build(student, identityNumber), verb, student, objectId, "Enrollment", sourceId);
        }
    }

    private async Task QueueAsync(XApiStatement stmt, string verb, User student, string objectId, string sourceType, int sourceId)
    {
        var (ok, error) = NelcStatementValidator.Validate(stmt);
        if (!ok)
        {
            _logger.LogWarning("Dropping non-compliant LRS statement (Verb={Verb}, Object={Object}): {Error}", verb, objectId, error);
            return;
        }

        var statementJson = JsonSerializer.Serialize(stmt);
        var actorIdentifier = !string.IsNullOrEmpty(student.Email) ? student.Email : $"user-{student.Id}";

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

    // Self-sufficient load so a dispatch never depends on how the caller hydrated the enrollment
    // (Participants / CourseSchedules must be present for the journey + registration duration).
    private Task<Enrollment?> LoadEnrollmentAsync(int enrollmentId, CancellationToken ct) =>
        _db.Enrollments
            .Include(e => e.Course)
            .Include(e => e.Participants)
            .Include(e => e.CourseSchedules)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);

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
