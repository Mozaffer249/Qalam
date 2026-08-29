using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class AdminSessionReadRepository : IAdminSessionReadRepository
{
    private readonly ApplicationDBContext _context;
    private readonly LiveSessionSettings _liveSettings;

    public AdminSessionReadRepository(
        ApplicationDBContext context,
        IOptions<LiveSessionSettings> liveSettings)
    {
        _context = context;
        _liveSettings = liveSettings.Value;
    }

    public async Task<List<AdminSessionListItemDto>> ListAsync(
        AdminSessionListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var q = _context.CourseSchedules.AsNoTracking()
            .Include(s => s.Complaints)
            .Include(s => s.Enrollment)
                .ThenInclude(e => e!.Course)
            .Include(s => s.Enrollment)
                .ThenInclude(e => e!.Participants)
                    .ThenInclude(p => p.Student)
            .AsQueryable();

        if (filter.Status.HasValue)
            q = q.Where(s => s.Status == filter.Status.Value);
        if (filter.EnrollmentId.HasValue)
            q = q.Where(s => s.EnrollmentId == filter.EnrollmentId.Value);
        if (filter.TeacherId.HasValue)
            q = q.Where(s => s.Enrollment.ApprovedByTeacherId == filter.TeacherId.Value
                             || (s.Enrollment.Course != null && s.Enrollment.Course.TeacherId == filter.TeacherId.Value));
        if (filter.StudentId.HasValue)
            q = q.Where(s => s.Enrollment.Participants.Any(p => p.StudentId == filter.StudentId.Value));
        if (filter.FromDate.HasValue)
            q = q.Where(s => s.Date >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            q = q.Where(s => s.Date <= filter.ToDate.Value);
        if (filter.HasComplaint == true)
            q = q.Where(s => s.Complaints.Any());
        if (filter.HasComplaint == false)
            q = q.Where(s => !s.Complaints.Any());

        var rows = await q
            .OrderByDescending(s => s.Date)
            .ThenByDescending(s => s.Id)
            .Take(500)
            .ToListAsync(cancellationToken);

        var scheduleIds = rows.Select(s => s.Id).ToList();
        var earnings = await _context.TeacherEarningLines.AsNoTracking()
            .Where(l => l.CourseScheduleId != null && scheduleIds.Contains(l.CourseScheduleId.Value))
            .ToDictionaryAsync(l => l.CourseScheduleId!.Value, cancellationToken);

        var sessionNumbers = await BuildSessionNumbersAsync(rows.Select(r => r.EnrollmentId).Distinct().ToList(), cancellationToken);

        var availabilityIds = rows.Select(s => s.TeacherAvailabilityId).Distinct().ToList();
        var startTimes = availabilityIds.Count == 0
            ? new Dictionary<int, TimeSpan?>()
            : await _context.TeacherAvailabilities.AsNoTracking()
                .Where(a => availabilityIds.Contains(a.Id))
                .Select(a => new { a.Id, a.TimeSlot.StartTime })
                .ToDictionaryAsync(a => a.Id, a => (TimeSpan?)a.StartTime, cancellationToken);

        var studentIds = rows
            .SelectMany(s => s.Enrollment.Participants.Select(p => p.StudentId))
            .Distinct()
            .ToList();
        var studentNames = studentIds.Count == 0
            ? new Dictionary<int, string?>()
            : await _context.Students.AsNoTracking()
                .Where(st => studentIds.Contains(st.Id))
                .Select(st => new
                {
                    st.Id,
                    Name = st.User == null
                        ? (string?)null
                        : ((st.User.FirstName ?? "") + " " + (st.User.LastName ?? "")).Trim(),
                })
                .ToDictionaryAsync(st => st.Id, st => string.IsNullOrWhiteSpace(st.Name) ? null : st.Name, cancellationToken);

        return rows.Select(s =>
        {
            earnings.TryGetValue(s.Id, out var line);
            var teacherId = s.Enrollment.ApprovedByTeacherId > 0
                ? s.Enrollment.ApprovedByTeacherId
                : s.Enrollment.Course?.TeacherId ?? 0;
            var primary = s.Enrollment.Participants.FirstOrDefault();
            sessionNumbers.TryGetValue(s.Id, out var sessionNumber);
            startTimes.TryGetValue(s.TeacherAvailabilityId, out var startTime);
            string? primaryStudentName = null;
            if (primary != null)
            {
                if (studentNames.TryGetValue(primary.StudentId, out var name) && !string.IsNullOrWhiteSpace(name))
                    primaryStudentName = name;
                else
                    primaryStudentName = FormatName(primary.Student);
            }
            return new AdminSessionListItemDto
            {
                ScheduleId = s.Id,
                EnrollmentId = s.EnrollmentId,
                SessionNumber = sessionNumber > 0 ? sessionNumber : s.Id,
                Date = s.Date,
                StartTime = startTime,
                DurationMinutes = s.DurationMinutes,
                Status = s.Status.ToString(),
                CourseTitle = s.Enrollment.Course?.Title,
                TeacherId = teacherId,
                PrimaryStudentName = primaryStudentName,
                HasOpenComplaint = s.Complaints.Any(c => SessionComplaintRules.IsBlockingStatus(c.Status)),
                ComplaintCount = s.Complaints.Count,
                AccruedAmount = line?.Amount,
                EarningLineStatus = line?.Status.ToString(),
            };
        }).ToList();
    }

    public async Task<AdminSessionDetailDto?> GetDetailAsync(
        int scheduleId,
        CancellationToken cancellationToken = default)
    {
        var s = await _context.CourseSchedules.AsNoTracking()
            .Include(x => x.Enrollment)
                .ThenInclude(e => e!.Course)
            .Include(x => x.Enrollment)
                .ThenInclude(e => e!.Participants)
                    .ThenInclude(p => p.Student)
                        .ThenInclude(st => st!.User)
            .Include(x => x.Enrollment)
                .ThenInclude(e => e!.PricingSnapshot)
            .Include(x => x.TeacherAvailability)
                .ThenInclude(a => a!.TimeSlot)
            .Include(x => x.TeachingMode)
            .Include(x => x.Attendances)
            .Include(x => x.LivePresenceEvents)
            .Include(x => x.Complaints)
                .ThenInclude(c => c.Attachments)
            .FirstOrDefaultAsync(x => x.Id == scheduleId, cancellationToken);
        if (s == null)
            return null;

        var teacherId = s.Enrollment.ApprovedByTeacherId > 0
            ? s.Enrollment.ApprovedByTeacherId
            : s.Enrollment.Course?.TeacherId ?? 0;
        var teacherName = teacherId > 0
            ? await _context.Teachers.AsNoTracking()
                .Where(t => t.Id == teacherId)
                .Select(t => t.User == null
                    ? ""
                    : ((t.User.FirstName ?? "") + " " + (t.User.LastName ?? "")).Trim())
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var sessionNumber = await ResolveSessionNumberAsync(s.EnrollmentId, s.Id, cancellationToken);
        var earningLine = await _context.TeacherEarningLines.AsNoTracking()
            .FirstOrDefaultAsync(l => l.CourseScheduleId == s.Id && l.Status != TeacherEarningLineStatus.Voided, cancellationToken);
        var refundCount = await _context.Refunds.AsNoTracking().CountAsync(r => r.EnrollmentId == s.EnrollmentId, cancellationToken);
        var reviews = await _context.TeacherReviews.AsNoTracking()
            .Where(r => r.SessionId == s.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AdminSessionReviewDto
            {
                ReviewId = r.Id,
                StudentId = r.StudentId,
                Rating = r.Rating,
                Feedback = r.Feedback,
                CreatedAt = r.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var timeline = await _context.SessionAuditLogs.AsNoTracking()
            .Where(l => l.CourseScheduleId == s.Id)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new AdminSessionAuditEntryDto
            {
                Id = l.Id,
                ActionType = l.ActionType.ToString(),
                ActorUserId = l.ActorUserId,
                ActorRole = l.ActorRole,
                PayloadJson = l.PayloadJson,
                CreatedAt = l.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var actualMinutes = s.StartedAt.HasValue && s.EndedAt.HasValue
            ? (int)Math.Max(0, (s.EndedAt.Value - s.StartedAt.Value).TotalMinutes)
            : (int?)null;

        return new AdminSessionDetailDto
        {
            ScheduleId = s.Id,
            EnrollmentId = s.EnrollmentId,
            SessionNumber = sessionNumber,
            Status = s.Status.ToString(),
            Date = s.Date,
            StartTime = s.TeacherAvailability?.TimeSlot?.StartTime,
            EndTime = s.TeacherAvailability?.TimeSlot?.EndTime,
            DurationMinutes = s.DurationMinutes,
            ActualDurationMinutes = actualMinutes,
            StartedAt = s.StartedAt,
            EndedAt = s.EndedAt,
            TeachingMode = s.TeachingMode?.Code ?? "",
            SessionType = s.Enrollment.Participants.Count > 1 ? "group" : "individual",
            CourseTitle = s.Enrollment.Course?.Title,
            TeacherId = teacherId,
            TeacherName = string.IsNullOrWhiteSpace(teacherName) ? null : teacherName,
            TeacherNote = s.TeacherNote,
            MeetingUrl = string.Equals(s.TeachingMode?.Code, "online", StringComparison.OrdinalIgnoreCase)
                ? _liveSettings.LiveKit.Url
                : null,
            LiveRoomName = LiveSessionRoomNames.ForSchedule(s.Id),
            Currency = s.Enrollment.PricingSnapshot?.Currency ?? "SAR",
            TeacherAttendance = new AdminSessionTeacherAttendanceDto
            {
                Status = s.TeacherAttendanceStatus.ToString(),
                JoinedAt = s.TeacherJoinedAt,
                LeftAt = s.TeacherLeftAt,
                InRoom = s.TeacherInRoom,
            },
            Students = s.Enrollment.Participants.Select(p =>
            {
                var att = s.Attendances.FirstOrDefault(a => a.StudentId == p.StudentId);
                return new AdminSessionStudentAttendanceDto
                {
                    StudentId = p.StudentId,
                    StudentName = FormatName(p.Student),
                    Status = (att?.Status ?? SessionAttendanceStatus.Pending).ToString(),
                    JoinedAt = att?.JoinedAt,
                    Rating = att?.Rating,
                    Note = att?.Note,
                };
            }).ToList(),
            LivePresenceEvents = s.LivePresenceEvents
                .OrderBy(e => e.OccurredAt)
                .Select(e => new AdminSessionLiveEventDto
                {
                    Role = e.Role.ToString(),
                    ParticipantId = e.ParticipantId,
                    ParticipantName = e.Role == LivePresenceRole.Teacher
                        ? (teacherName ?? "Teacher")
                        : s.Enrollment.Participants.FirstOrDefault(p => p.StudentId == e.ParticipantId) is { } p
                            ? FormatName(p.Student) ?? $"Student {e.ParticipantId}"
                            : $"Student {e.ParticipantId}",
                    EventType = e.EventType.ToString(),
                    OccurredAt = e.OccurredAt,
                }).ToList(),
            Reviews = reviews,
            Complaints = s.Complaints.OrderByDescending(c => c.FiledAt).Select(c => new AdminSessionComplaintDto
            {
                ComplaintId = c.Id,
                StudentId = c.StudentId,
                ReasonCode = c.ReasonCode.ToString(),
                Description = c.Description,
                Status = c.Status.ToString(),
                FiledAt = c.FiledAt,
                ResolutionCode = c.ResolutionCode?.ToString(),
                ResolutionNotes = c.ResolutionNotes,
                RequiresTeacherResponse = c.RequiresTeacherResponse,
                TeacherResponse = c.TeacherResponse,
                Attachments = c.Attachments.Select(a => new AdminSessionComplaintAttachmentDto
                {
                    AttachmentId = a.Id,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    ContentType = a.ContentType,
                }).ToList(),
            }).ToList(),
            Finance = new AdminSessionFinanceDto
            {
                AccruedAmount = earningLine?.Amount,
                EarningLineKey = earningLine != null ? $"earn-{earningLine.Id}" : null,
                EarningLineStatus = earningLine?.Status.ToString(),
                RefundCount = refundCount,
            },
            Timeline = timeline,
        };
    }

    private async Task<Dictionary<int, int>> BuildSessionNumbersAsync(
        IReadOnlyList<int> enrollmentIds,
        CancellationToken cancellationToken)
    {
        if (enrollmentIds.Count == 0)
            return new Dictionary<int, int>();

        var schedules = await _context.CourseSchedules.AsNoTracking()
            .Where(s => enrollmentIds.Contains(s.EnrollmentId)
                        && s.Status != ScheduleStatus.Cancelled
                        && s.Status != ScheduleStatus.Rescheduled)
            .OrderBy(s => s.EnrollmentId)
            .ThenBy(s => s.Date)
            .ThenBy(s => s.Id)
            .Select(s => new { s.Id, s.EnrollmentId })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<int, int>();
        foreach (var group in schedules.GroupBy(s => s.EnrollmentId))
        {
            var i = 1;
            foreach (var row in group)
                result[row.Id] = i++;
        }
        return result;
    }

    private async Task<int> ResolveSessionNumberAsync(
        int enrollmentId,
        int scheduleId,
        CancellationToken cancellationToken)
    {
        var map = await BuildSessionNumbersAsync([enrollmentId], cancellationToken);
        return map.TryGetValue(scheduleId, out var n) ? n : 1;
    }

    private static string? FormatName(Data.Entity.Student.Student? student)
    {
        if (student?.User == null)
            return student == null ? null : $"#{student.Id}";
        var name = $"{student.User.FirstName} {student.User.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? $"#{student.Id}" : name;
    }
}
