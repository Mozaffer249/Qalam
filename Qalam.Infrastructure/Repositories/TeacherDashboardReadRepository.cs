using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class TeacherDashboardReadRepository : ITeacherDashboardReadRepository
{
    private readonly ApplicationDBContext _context;
    private readonly SessionSettings _sessionSettings;

    public TeacherDashboardReadRepository(
        ApplicationDBContext context,
        IOptions<SessionSettings> sessionSettings)
    {
        _context = context;
        _sessionSettings = sessionSettings.Value;
    }

    public async Task<List<TeacherMySessionListItemDto>> GetMySessionsAsync(
        int teacherId,
        string filter,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(nowUtc);
        var currentTime = nowUtc.TimeOfDay;
        var filterKey = filter.ToLowerInvariant();
        var take = Math.Clamp(pageSize, 1, 50);

        var openQuery = _context.ScheduledSessions
            .AsNoTracking()
            .Where(ss => ss.Session.TeacherId == teacherId);

        openQuery = filterKey switch
        {
            "past" => openQuery.Where(ss =>
                ss.Date < today || (ss.Date == today && ss.TimeSlot.StartTime < currentTime)),
            "all" => openQuery,
            _ => openQuery.Where(ss =>
                ss.Date > today || (ss.Date == today && ss.TimeSlot.StartTime >= currentTime)),
        };

        var openRows = await openQuery
            .Select(ss => new SessionRowProjection
            {
                Id = ss.Id,
                CourseTitle = ss.Session.SessionRequest.Subject != null
                    ? ss.Session.SessionRequest.Subject.NameAr
                    : "جلسة",
                SourceLabel = "طلب جلسة #" + ss.Session.SessionRequestId,
                SessionNumber = ss.SessionId,
                SessionTitle = ss.Session.SessionRequest.Subject != null
                    ? ss.Session.SessionRequest.Subject.NameEn
                    : "Session",
                Date = ss.Date,
                StartTime = ss.TimeSlot.StartTime,
                DurationMinutes = ss.TimeSlot.DurationMinutes > 0
                    ? ss.TimeSlot.DurationMinutes
                    : (int)(ss.TimeSlot.EndTime - ss.TimeSlot.StartTime).TotalMinutes,
                TeachingModeOnline = ss.TeachingMode.Code == "online",
                SessionTypeGroup = ss.Session.SessionRequest.SessionType.Code == "group",
                StudentsCount = 1,
                Status = ss.Status,
            })
            .ToListAsync(cancellationToken);

        var courseQuery = _context.CourseSchedules
            .AsNoTracking()
            .Where(cs =>
                cs.Enrollment.ApprovedByTeacherId == teacherId
                || (cs.Enrollment.Course != null && cs.Enrollment.Course.TeacherId == teacherId));

        courseQuery = filterKey switch
        {
            "past" => courseQuery.Where(cs =>
                cs.Date < today
                || (cs.Date == today && cs.TeacherAvailability.TimeSlot.StartTime < currentTime)
                || cs.Status == ScheduleStatus.Completed
                || cs.Status == ScheduleStatus.Cancelled
                || cs.Status == ScheduleStatus.Rescheduled),
            "all" => courseQuery,
            _ => courseQuery.Where(cs =>
                (cs.Status == ScheduleStatus.Scheduled || cs.Status == ScheduleStatus.InProgress)
                && (cs.Date > today
                    || (cs.Date == today && cs.TeacherAvailability.TimeSlot.StartTime >= currentTime))),
        };

        var courseRows = await courseQuery
            .Select(cs => new SessionRowProjection
            {
                Id = cs.Id,
                CourseTitle = cs.Enrollment.Course != null ? cs.Enrollment.Course.Title : "Course session",
                SourceLabel = "Course enrollment #" + cs.EnrollmentId,
                SessionNumber = cs.CourseSession != null ? cs.CourseSession.SessionNumber : cs.Id,
                SessionTitle = cs.CourseSession != null && cs.CourseSession.Title != null
                    ? cs.CourseSession.Title
                    : (cs.Enrollment.Course != null ? cs.Enrollment.Course.Title : "Session"),
                Date = cs.Date,
                StartTime = cs.TeacherAvailability.TimeSlot.StartTime,
                DurationMinutes = cs.DurationMinutes,
                TeachingModeOnline = cs.TeachingMode.Code == "online",
                SessionTypeGroup = cs.Enrollment.Participants.Count > 1,
                StudentsCount = cs.Enrollment.Participants.Count,
                Status = cs.Status,
            })
            .ToListAsync(cancellationToken);

        IEnumerable<SessionRowProjection> merged = openRows.Concat(courseRows);
        merged = filterKey is "past" or "all"
            ? merged.OrderByDescending(r => r.Date).ThenByDescending(r => r.StartTime)
            : merged.OrderBy(r => r.Date).ThenBy(r => r.StartTime);

        return merged.Take(take).Select(MapSessionRow).ToList();
    }

    public async Task<TeacherMySessionDetailDto?> GetMySessionByIdAsync(
        int teacherId,
        int scheduledSessionId,
        CancellationToken cancellationToken = default)
    {
        // Prefer CourseSchedule so enrollment live presence / attendance is not shadowed
        // when a ScheduledSession happens to share the same id.
        var courseDetail = await _context.CourseSchedules
            .AsNoTracking()
            .Where(cs => cs.Id == scheduledSessionId &&
                (cs.Enrollment.ApprovedByTeacherId == teacherId ||
                 (cs.Enrollment.Course != null && cs.Enrollment.Course.TeacherId == teacherId)))
            .Select(cs => new
            {
                cs.Id,
                CourseTitle = cs.Enrollment.Course != null ? cs.Enrollment.Course.Title : "Course session",
                cs.Date,
                StartTime = cs.TeacherAvailability.TimeSlot.StartTime,
                cs.DurationMinutes,
                TeachingModeOnline = cs.TeachingMode.Code == "online",
                Status = cs.Status,
                cs.TeacherNote,
                cs.EndedAt,
                Participants = cs.Enrollment.Participants.Select(p => new
                {
                    p.StudentId,
                    StudentName = ((p.Student.User!.FirstName ?? "") + " " + (p.Student.User.LastName ?? "")).Trim(),
                    AttendanceStatus = cs.Attendances
                        .Where(a => a.StudentId == p.StudentId)
                        .Select(a => (SessionAttendanceStatus?)a.Status)
                        .FirstOrDefault(),
                    JoinedAt = cs.Attendances
                        .Where(a => a.StudentId == p.StudentId)
                        .Select(a => a.JoinedAt)
                        .FirstOrDefault(),
                    Rating = cs.Attendances
                        .Where(a => a.StudentId == p.StudentId)
                        .Select(a => a.Rating)
                        .FirstOrDefault(),
                    Note = cs.Attendances
                        .Where(a => a.StudentId == p.StudentId)
                        .Select(a => a.Note)
                        .FirstOrDefault(),
                }).ToList(),
                cs.TeacherAttendanceStatus,
                cs.TeacherJoinedAt,
                cs.TeacherLeftAt,
                cs.TeacherInRoom,
                EnrollmentStatus = cs.Enrollment.EnrollmentStatus,
                EndTime = cs.TeacherAvailability.TimeSlot.EndTime,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (courseDetail != null)
        {
            var participants = courseDetail.Participants
                .Select(p => new TeacherSessionStudentDto
                {
                    StudentId = p.StudentId,
                    StudentName = p.StudentName,
                    Attendance = p.AttendanceStatus?.ToString() ?? "Pending",
                    JoinedAt = p.JoinedAt,
                    Rating = p.Rating,
                    Note = p.Note,
                })
                .ToList();

            var studentNames = courseDetail.Participants
                .ToDictionary(p => p.StudentId, p => string.IsNullOrWhiteSpace(p.StudentName) ? $"Student {p.StudentId}" : p.StudentName);

            var presenceEvents = await _context.SessionLivePresenceEvents
                .AsNoTracking()
                .Where(e => e.CourseScheduleId == courseDetail.Id)
                .OrderByDescending(e => e.OccurredAt)
                .Take(100)
                .Select(e => new
                {
                    e.Role,
                    e.ParticipantId,
                    e.EventType,
                    e.OccurredAt,
                })
                .ToListAsync(cancellationToken);

            var livePresenceEvents = presenceEvents
                .Select(e => new SessionLivePresenceEventDto
                {
                    Role = e.Role.ToString(),
                    ParticipantId = e.ParticipantId,
                    ParticipantName = e.Role == LivePresenceRole.Teacher
                        ? "Teacher"
                        : studentNames.TryGetValue(e.ParticipantId, out var name)
                            ? name
                            : $"Student {e.ParticipantId}",
                    EventType = e.EventType.ToString(),
                    OccurredAt = e.OccurredAt,
                })
                .ToList();

            var studentsCount = participants.Count > 0 ? participants.Count : 1;
            var startsAt = PlatformTime.ToUtc(courseDetail.Date, courseDetail.StartTime);
            var canJoin = SessionJoinRules.CanJoinUtc(
                courseDetail.EnrollmentStatus,
                courseDetail.Status,
                courseDetail.Date,
                courseDetail.StartTime,
                courseDetail.EndTime,
                DateTime.UtcNow,
                _sessionSettings.EnforceJoinWindow);

            return new TeacherMySessionDetailDto
            {
                Id = courseDetail.Id,
                CourseTitle = courseDetail.CourseTitle,
                SourceLabel = "Course enrollment",
                SessionNumber = 1,
                SessionTitle = courseDetail.CourseTitle,
                StartsAt = startsAt,
                DurationMinutes = courseDetail.DurationMinutes,
                TeachingMode = courseDetail.TeachingModeOnline ? "Online" : "InPerson",
                SessionType = studentsCount > 1 ? "Group" : "Individual",
                StudentsCount = studentsCount,
                Status = MapScheduleStatus(courseDetail.Status),
                Notes = courseDetail.TeacherNote,
                EndedAt = courseDetail.EndedAt,
                Students = participants,
                CanJoin = canJoin,
                TeacherAttendance = courseDetail.TeacherAttendanceStatus.ToString(),
                TeacherJoinedAt = courseDetail.TeacherJoinedAt,
                TeacherLeftAt = courseDetail.TeacherLeftAt,
                TeacherInRoom = courseDetail.TeacherInRoom,
                LivePresenceEvents = livePresenceEvents,
            };
        }

        var row = await _context.ScheduledSessions
            .AsNoTracking()
            .Where(ss => ss.Session.TeacherId == teacherId && ss.Id == scheduledSessionId)
            .Select(ss => new SessionRowProjection
            {
                Id = ss.Id,
                CourseTitle = ss.Session.SessionRequest.Subject != null
                    ? ss.Session.SessionRequest.Subject.NameAr
                    : "جلسة",
                SourceLabel = "طلب جلسة #" + ss.Session.SessionRequestId,
                SessionNumber = ss.SessionId,
                SessionTitle = ss.Session.SessionRequest.Subject != null
                    ? ss.Session.SessionRequest.Subject.NameEn
                    : "Session",
                Date = ss.Date,
                StartTime = ss.TimeSlot.StartTime,
                DurationMinutes = ss.TimeSlot.DurationMinutes > 0
                    ? ss.TimeSlot.DurationMinutes
                    : (int)(ss.TimeSlot.EndTime - ss.TimeSlot.StartTime).TotalMinutes,
                TeachingModeOnline = ss.TeachingMode.Code == "online",
                SessionTypeGroup = ss.Session.SessionRequest.SessionType.Code == "group",
                Status = ss.Status,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null)
            return null;

        var item = MapSessionRow(row);

        var student = await _context.ScheduledSessions
            .AsNoTracking()
            .Where(ss => ss.Id == scheduledSessionId)
            .Select(ss => new
            {
                ss.Session.StudentId,
                Name = (ss.Session.Student.User!.FirstName ?? "") + " " + (ss.Session.Student.User.LastName ?? ""),
                ss.Session.SessionRequest.Description,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new TeacherMySessionDetailDto
        {
            Id = item.Id,
            CourseTitle = item.CourseTitle,
            SourceLabel = item.SourceLabel,
            SessionNumber = item.SessionNumber,
            SessionTitle = item.SessionTitle,
            StartsAt = item.StartsAt,
            DurationMinutes = item.DurationMinutes,
            TeachingMode = item.TeachingMode,
            SessionType = item.SessionType,
            StudentsCount = item.StudentsCount,
            Status = item.Status,
            Description = student?.Description,
            Students = student == null
                ? new List<TeacherSessionStudentDto>()
                : new List<TeacherSessionStudentDto>
                {
                    new()
                    {
                        StudentId = student.StudentId,
                        StudentName = student.Name.Trim(),
                        Attendance = "Pending",
                    }
                },
        };
    }

    public async Task<TeacherFinanceSummaryDto> GetFinanceSummaryAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var payments = await GetTeacherPaymentRowsAsync(teacherId, cancellationToken);
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var succeeded = payments.Where(p => p.Status == PaymentStatus.Succeeded).ToList();
        var refunded = payments.Where(p => p.Status == PaymentStatus.Refunded).ToList();

        return new TeacherFinanceSummaryDto
        {
            TotalEarningsAllTime = succeeded.Sum(p => p.Amount),
            EarningsThisMonth = succeeded.Where(p => p.CreatedAt >= thisMonthStart).Sum(p => p.Amount),
            EarningsLastMonth = succeeded.Where(p => p.CreatedAt >= lastMonthStart && p.CreatedAt < thisMonthStart).Sum(p => p.Amount),
            PendingPayout = 0,
            NextPayoutDate = null,
            PlatformFeesThisMonth = 0,
            RefundsThisMonth = refunded.Where(p => p.CreatedAt >= thisMonthStart).Sum(p => p.Amount),
            TransactionsCount = payments.Count,
        };
    }

    public async Task<PaginatedResult<TeacherFinanceTransactionDto>> GetFinanceTransactionsAsync(
        int teacherId,
        string? typeFilter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var payments = await GetTeacherPaymentRowsAsync(teacherId, cancellationToken);
        var items = payments
            .Select(p => new TeacherFinanceTransactionDto
            {
                Id = $"tx-{p.PaymentId}",
                Type = p.Status == PaymentStatus.Refunded ? "Refund" : "Payment",
                Status = p.Status switch
                {
                    PaymentStatus.Succeeded => "Completed",
                    PaymentStatus.Pending => "Pending",
                    PaymentStatus.Refunded => "Completed",
                    _ => "Failed",
                },
                Amount = p.Status == PaymentStatus.Refunded ? -p.Amount : p.Amount,
                Currency = "SAR",
                CreatedAt = p.CreatedAt,
                Description = p.Description,
                RelatedStudentName = p.StudentName,
                RelatedCourseTitle = p.CourseTitle,
                InvoiceNumber = p.InvoiceNumber,
            })
            .Where(t => string.IsNullOrEmpty(typeFilter) || typeFilter == "all" || t.Type.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        pageSize = Math.Clamp(pageSize, 1, 50);
        pageNumber = Math.Max(1, pageNumber);
        var total = items.Count;
        var page = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PaginatedResult<TeacherFinanceTransactionDto>(page, total, pageNumber, pageSize);
    }

    public async Task<TeacherNotificationsPageDto> GetNotificationsAsync(
        int teacherId,
        bool unreadOnly,
        CancellationToken cancellationToken = default)
    {
        var targets = await _context.OpenSessionRequestTargets
            .AsNoTracking()
            .Where(t => t.TeacherId == teacherId)
            .Where(t => t.Status != OpenSessionRequestTargetStatus.Skipped)
            .Where(t => t.OpenSessionRequest.Status == OpenSessionRequestStatus.Active
                         || t.OpenSessionRequest.Status == OpenSessionRequestStatus.ReceivingOffers)
            .OrderByDescending(t => t.MatchedAt)
            .Take(50)
            .Select(t => new
            {
                t.SessionRequestId,
                t.Status,
                t.MatchedAt,
                t.ViewedAt,
                SubjectAr = t.OpenSessionRequest.Subject != null ? t.OpenSessionRequest.Subject.NameAr : "طلب جلسة",
                SubjectEn = t.OpenSessionRequest.Subject != null ? t.OpenSessionRequest.Subject.NameEn : "Session request",
            })
            .ToListAsync(cancellationToken);

        var items = targets.Select(t => new TeacherNotificationDto
        {
            Id = t.SessionRequestId,
            Type = "NewQualifiedRequest",
            TitleAr = "طلب جلسة جديد",
            TitleEn = "New session request",
            BodyAr = t.SubjectAr ?? "طلب جلسة جديد يطابق تخصصك",
            BodyEn = t.SubjectEn ?? "A new session request matches your subjects",
            CreatedAt = t.MatchedAt,
            ReadAt = t.Status == OpenSessionRequestTargetStatus.Notified ? null : t.ViewedAt ?? t.MatchedAt,
            RequestId = t.SessionRequestId,
        }).ToList();

        if (unreadOnly)
            items = items.Where(i => i.ReadAt == null).ToList();

        return new TeacherNotificationsPageDto
        {
            Items = items,
            Counts = new TeacherNotificationCountsDto
            {
                All = targets.Count,
                Unread = targets.Count(t => t.Status == OpenSessionRequestTargetStatus.Notified),
            },
        };
    }

    public async Task<bool> MarkNotificationReadAsync(
        int teacherId,
        int notificationId,
        CancellationToken cancellationToken = default)
    {
        var target = await _context.OpenSessionRequestTargets
            .FirstOrDefaultAsync(
                t => t.TeacherId == teacherId && t.SessionRequestId == notificationId,
                cancellationToken);

        if (target == null)
            return false;

        if (target.Status == OpenSessionRequestTargetStatus.Notified)
        {
            target.Status = OpenSessionRequestTargetStatus.Viewed;
            target.ViewedAt = DateTime.UtcNow;
            target.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<int> MarkAllNotificationsReadAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var targets = await _context.OpenSessionRequestTargets
            .Where(t => t.TeacherId == teacherId && t.Status == OpenSessionRequestTargetStatus.Notified)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var target in targets)
        {
            target.Status = OpenSessionRequestTargetStatus.Viewed;
            target.ViewedAt = now;
            target.UpdatedAt = now;
        }

        if (targets.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return targets.Count;
    }

    private static TeacherMySessionListItemDto MapSessionRow(SessionRowProjection row) =>
        new()
        {
            Id = row.Id,
            CourseTitle = row.CourseTitle,
            SourceLabel = row.SourceLabel,
            SessionNumber = row.SessionNumber,
            SessionTitle = row.SessionTitle,
            StartsAt = PlatformTime.ToUtc(row.Date, row.StartTime),
            DurationMinutes = row.DurationMinutes,
            TeachingMode = row.TeachingModeOnline ? "Online" : "InPerson",
            SessionType = row.SessionTypeGroup ? "Group" : "Individual",
            StudentsCount = row.StudentsCount > 0 ? row.StudentsCount : 1,
            Status = MapScheduleStatus(row.Status),
        };

    private static string MapScheduleStatus(ScheduleStatus status) => status switch
    {
        ScheduleStatus.Completed => "Completed",
        ScheduleStatus.Cancelled => "Cancelled",
        ScheduleStatus.Rescheduled => "Rescheduled",
        ScheduleStatus.InProgress => "InProgress",
        _ => "Scheduled",
    };

    private sealed class SessionRowProjection
    {
        public int Id { get; init; }
        public string CourseTitle { get; init; } = "";
        public string SourceLabel { get; init; } = "";
        public int SessionNumber { get; init; }
        public string SessionTitle { get; init; } = "";
        public DateOnly Date { get; init; }
        public TimeSpan StartTime { get; init; }
        public int DurationMinutes { get; init; }
        public bool TeachingModeOnline { get; init; }
        public bool SessionTypeGroup { get; init; }
        public int StudentsCount { get; init; } = 1;
        public ScheduleStatus Status { get; init; }
    }

    private async Task<List<TeacherPaymentRow>> GetTeacherPaymentRowsAsync(
        int teacherId,
        CancellationToken cancellationToken)
    {
        return await _context.EnrollmentPayments
            .AsNoTracking()
            .Where(ep => ep.EnrollmentParticipant.Enrollment.ApprovedByTeacherId == teacherId
                         || (ep.EnrollmentParticipant.Enrollment.Course != null
                             && ep.EnrollmentParticipant.Enrollment.Course.TeacherId == teacherId))
            .Select(ep => new TeacherPaymentRow
            {
                PaymentId = ep.PaymentId,
                Amount = ep.Payment.TotalAmount,
                Status = ep.Payment.Status,
                CreatedAt = ep.Payment.CreatedAt,
                Description = ep.Payment.PaymentItems.Select(pi => pi.Description).FirstOrDefault() ?? "Enrollment payment",
                StudentName = ep.EnrollmentParticipant.Student.User != null
                    ? (ep.EnrollmentParticipant.Student.User.FirstName ?? "") + " " + (ep.EnrollmentParticipant.Student.User.LastName ?? "")
                    : null,
                CourseTitle = ep.EnrollmentParticipant.Enrollment.Course != null
                    ? ep.EnrollmentParticipant.Enrollment.Course.Title
                    : null,
                InvoiceNumber = ep.Payment.InvoiceNumber,
            })
            .ToListAsync(cancellationToken);
    }

    private sealed class TeacherPaymentRow
    {
        public int PaymentId { get; init; }
        public decimal Amount { get; init; }
        public PaymentStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public string Description { get; init; } = "";
        public string? StudentName { get; init; }
        public string? CourseTitle { get; init; }
        public string? InvoiceNumber { get; init; }
    }
}
