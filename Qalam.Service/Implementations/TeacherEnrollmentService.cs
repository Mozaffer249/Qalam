using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Helpers;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Mappers;

namespace Qalam.Service.Implementations;

public class TeacherEnrollmentService : ITeacherEnrollmentService
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IEnrollmentConversationRepository _conversationRepository;
    private readonly UserManager<User> _userManager;
    private readonly IRabbitMQService _rabbitMq;
    private readonly PaymentSettings _paymentSettings;
    private readonly SessionSettings _sessionSettings;
    private readonly ILogger<TeacherEnrollmentService> _logger;

    public TeacherEnrollmentService(
        ITeacherRepository teacherRepository,
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository,
        IEnrollmentConversationRepository conversationRepository,
        UserManager<User> userManager,
        IRabbitMQService rabbitMq,
        IOptions<PaymentSettings> paymentSettings,
        IOptions<SessionSettings> sessionSettings,
        ILogger<TeacherEnrollmentService> logger)
    {
        _teacherRepository = teacherRepository;
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
        _conversationRepository = conversationRepository;
        _userManager = userManager;
        _rabbitMq = rabbitMq;
        _paymentSettings = paymentSettings.Value;
        _sessionSettings = sessionSettings.Value;
        _logger = logger;
    }

    public async Task<PaginatedResult<TeacherEnrollmentListItemDto>?> GetEnrollmentsForTeacherAsync(
        int userId,
        EnrollmentStatus? status,
        EnrollmentSource? source,
        EnrollmentKind? kind,
        TeacherEnrollmentSourceBadge? sourceBadge,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            return null;

        var query = _enrollmentRepository.GetTeacherListQueryable(teacher.Id);

        if (status.HasValue)
            query = query.Where(e => e.EnrollmentStatus == status.Value);

        if (source.HasValue)
            query = query.Where(e => e.Source == source.Value);

        if (kind.HasValue)
            query = query.Where(e => e.Kind == kind.Value);

        var searchTerm = search?.Trim();
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var pattern = $"%{searchTerm}%";
            query = query.Where(e =>
                (e.Course != null && EF.Functions.Like(e.Course.Title, pattern))
                || (e.OpenSessionRequest != null
                    && e.OpenSessionRequest.Subject != null
                    && (EF.Functions.Like(e.OpenSessionRequest.Subject.NameEn, pattern)
                        || (e.OpenSessionRequest.Subject.NameAr != null
                            && EF.Functions.Like(e.OpenSessionRequest.Subject.NameAr, pattern))))
                || e.Participants.Any(p =>
                    p.Student != null
                    && p.Student.User != null
                    && (EF.Functions.Like(p.Student.User.FirstName, pattern)
                        || EF.Functions.Like(p.Student.User.LastName, pattern)
                        || EF.Functions.Like(
                            p.Student.User.FirstName + " " + p.Student.User.LastName,
                            pattern)))
                || (e.LeaderStudent != null
                    && e.LeaderStudent.User != null
                    && (EF.Functions.Like(e.LeaderStudent.User.FirstName, pattern)
                        || EF.Functions.Like(e.LeaderStudent.User.LastName, pattern))));
        }

        var enrollments = await query
            .OrderByDescending(e => e.ActivatedAt ?? e.ApprovedAt)
            .ToListAsync(cancellationToken);

        var currency = _paymentSettings.DefaultCurrency;
        var mapped = enrollments
            .Select(e => TeacherEnrollmentMapping.ToListItem(e, currency))
            .ToList();

        if (sourceBadge.HasValue)
            mapped = mapped.Where(x => x.SourceBadge == sourceBadge.Value).ToList();

        var totalCount = mapped.Count;
        var size = pageSize <= 0 ? 20 : pageSize;
        var number = pageNumber <= 0 ? 1 : pageNumber;
        var page = mapped
            .Skip((number - 1) * size)
            .Take(size)
            .ToList();

        return new PaginatedResult<TeacherEnrollmentListItemDto>(page, totalCount, number, size);
    }

    public async Task<PaginatedResult<TeacherEnrollmentListItemDto>?> GetCourseEnrollmentsAsync(
        int userId,
        int courseId,
        EnrollmentStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            return null;

        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null || course.TeacherId != teacher.Id)
            return null;

        var query = _enrollmentRepository.GetCourseListQueryable(courseId);

        if (status.HasValue)
            query = query.Where(e => e.EnrollmentStatus == status.Value);

        var enrollments = await query
            .OrderByDescending(e => e.ActivatedAt ?? e.ApprovedAt)
            .ToListAsync(cancellationToken);

        var totalCount = enrollments.Count;
        var currency = _paymentSettings.DefaultCurrency;
        var number = pageNumber <= 0 ? 1 : pageNumber;
        var size = pageSize <= 0 ? 20 : pageSize;

        var page = enrollments
            .Skip((number - 1) * size)
            .Take(size)
            .Select(e => TeacherEnrollmentMapping.ToListItem(e, currency))
            .ToList();

        return new PaginatedResult<TeacherEnrollmentListItemDto>(page, totalCount, number, size);
    }

    public async Task<TeacherEnrollmentDetailDto?> GetEnrollmentByIdAsync(
        int userId,
        int enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            return null;

        var enrollment = await _enrollmentRepository.GetByIdForTeacherDetailAsync(enrollmentId, cancellationToken);
        if (enrollment == null)
            return null;

        var ownsCourse = enrollment.Course != null && enrollment.Course.TeacherId == teacher.Id;
        var isApprover = enrollment.ApprovedByTeacherId == teacher.Id;
        if (!ownsCourse && !isApprover)
            return null;

        var totalAmount = enrollment.AmountDue > 0
            ? enrollment.AmountDue
            : enrollment.EnrollmentRequest?.EstimatedTotalPrice ?? 0m;
        var participantCount = enrollment.Participants.Count;
        var baseShare = participantCount > 0
            ? Math.Round(totalAmount / participantCount, 2, MidpointRounding.AwayFromZero)
            : 0m;
        var succeededCount = enrollment.Participants.Count(p => p.PaymentStatus == PaymentStatus.Succeeded);
        var amountPaid = TeacherEnrollmentMapping.ResolveAmountPaid(enrollment, totalAmount, succeededCount);

        var participants = enrollment.Participants
            .OrderBy(p => p.Id)
            .Select(p =>
            {
                var isLastPending = enrollment.Kind == EnrollmentKind.Group
                                 && p.PaymentStatus == PaymentStatus.Pending
                                 && enrollment.Participants.Count(x => x.PaymentStatus == PaymentStatus.Pending) == 1;
                var share = enrollment.Kind == EnrollmentKind.Individual
                    ? totalAmount
                    : (isLastPending ? totalAmount - (baseShare * succeededCount) : baseShare);

                return new TeacherEnrollmentParticipantDto
                {
                    ParticipantId = p.Id,
                    StudentId = p.StudentId,
                    StudentName = p.Student?.User != null
                        ? (p.Student.User.FirstName + " " + p.Student.User.LastName).Trim()
                        : null,
                    IsMinor = p.Student?.IsMinor ?? false,
                    PaymentStatus = p.PaymentStatus,
                    PaidAt = p.PaidAt,
                    Share = share
                };
            })
            .ToList();

        var isFlexible = enrollment.Course?.IsFlexible ?? false;
        var isDirected = enrollment.Source == EnrollmentSource.SessionRequest
                         && enrollment.OpenSessionRequest?.TargetedTeacherId != null;

        var paymentMethod = await _enrollmentRepository.GetSucceededPaymentProviderAsync(
            enrollment.Id, cancellationToken);

        var dto = new TeacherEnrollmentDetailDto
        {
            Id = enrollment.Id,
            CourseId = enrollment.CourseId ?? 0,
            CourseTitle = enrollment.Course?.Title
                          ?? enrollment.OpenSessionRequest?.Subject?.NameEn
                          ?? enrollment.OpenSessionRequest?.Subject?.NameAr
                          ?? string.Empty,
            TeachingModeNameEn = enrollment.Course?.TeachingMode?.NameEn
                                 ?? enrollment.OpenSessionRequest?.TeachingMode?.NameEn,
            SessionTypeNameEn = enrollment.Course?.SessionType?.NameEn,
            SubjectNameEn = enrollment.Course?.TeacherSubject?.Subject?.NameEn
                            ?? enrollment.OpenSessionRequest?.Subject?.NameEn,
            SubjectNameAr = enrollment.Course?.TeacherSubject?.Subject?.NameAr
                            ?? enrollment.OpenSessionRequest?.Subject?.NameAr,
            Kind = enrollment.Kind,
            LeaderStudentId = enrollment.LeaderStudentId,
            LeaderStudentName = enrollment.LeaderStudent?.User != null
                ? (enrollment.LeaderStudent.User.FirstName + " " + enrollment.LeaderStudent.User.LastName).Trim()
                : null,
            EnrollmentStatus = enrollment.EnrollmentStatus,
            ApprovedAt = enrollment.ApprovedAt,
            ActivatedAt = enrollment.ActivatedAt,
            PaymentDeadline = enrollment.PaymentDeadline,
            Source = enrollment.Source,
            IsFlexible = isFlexible,
            IsDirected = isDirected,
            SourceBadge = TeacherEnrollmentMapping.ResolveSourceBadge(
                enrollment.Source, isFlexible, isDirected),
            TotalAmount = totalAmount,
            AmountPaid = amountPaid,
            AmountRemaining = Math.Max(0, totalAmount - amountPaid),
            Currency = _paymentSettings.DefaultCurrency,
            Participants = participants,
            EnrollmentRequestId = enrollment.EnrollmentRequestId,
            SessionOfferId = enrollment.SessionOfferId,
            CoursePrice = enrollment.Course?.Price,
            PaymentMethod = paymentMethod,
        };

        var utcNow = DateTime.UtcNow;
        var schedules = enrollment.CourseSchedules
            .OrderBy(cs => cs.Date)
            .ThenBy(cs => cs.TeacherAvailability != null && cs.TeacherAvailability.TimeSlot != null
                ? cs.TeacherAvailability.TimeSlot.StartTime
                : TimeSpan.Zero)
            .ToList();

        var attended = 0;
        var absentOrLate = 0;

        for (var i = 0; i < schedules.Count; i++)
        {
            var cs = schedules[i];
            var slot = cs.TeacherAvailability?.TimeSlot;
            var duration = cs.DurationMinutes > 0
                ? cs.DurationMinutes
                : slot?.ResolveDurationMinutes() ?? 0;

            var sessionNumber = i + 1;
            var slotMatch = enrollment.EnrollmentRequest?.SelectedSessionSlots?
                .FirstOrDefault(ss =>
                    ss.SessionDate == cs.Date && ss.TeacherAvailabilityId == cs.TeacherAvailabilityId);
            if (slotMatch != null)
                sessionNumber = slotMatch.SessionNumber;

            string? unitName = null;
            string? lessonName = null;
            var units = new List<EnrollmentSessionContentUnitDto>();

            var courseSession = cs.CourseSession
                ?? enrollment.Course?.Sessions?.FirstOrDefault(s => s.SessionNumber == sessionNumber);
            if (courseSession?.Units != null)
            {
                foreach (var u in courseSession.Units)
                {
                    units.Add(new EnrollmentSessionContentUnitDto
                    {
                        Id = u.Id,
                        ContentUnitId = u.ContentUnitId,
                        ContentUnitName = u.ContentUnit?.NameEn ?? u.ContentUnit?.NameAr,
                        LessonId = u.LessonId,
                        LessonName = u.Lesson?.NameEn ?? u.Lesson?.NameAr,
                    });
                    unitName ??= u.ContentUnit?.NameAr ?? u.ContentUnit?.NameEn;
                    lessonName ??= u.Lesson?.NameAr ?? u.Lesson?.NameEn;
                }
            }

            if (slotMatch?.Units != null && units.Count == 0)
            {
                foreach (var u in slotMatch.Units)
                {
                    units.Add(new EnrollmentSessionContentUnitDto
                    {
                        Id = u.Id,
                        ContentUnitId = u.ContentUnitId,
                        ContentUnitName = u.ContentUnit?.NameEn ?? u.ContentUnit?.NameAr,
                        LessonId = u.LessonId,
                        LessonName = u.Lesson?.NameEn ?? u.Lesson?.NameAr,
                    });
                    unitName ??= u.ContentUnit?.NameAr ?? u.ContentUnit?.NameEn;
                    lessonName ??= u.Lesson?.NameAr ?? u.Lesson?.NameEn;
                }
            }

            var primaryAttendance = cs.Attendances == null || cs.Attendances.Count == 0
                ? null
                : enrollment.LeaderStudentId is int leaderId
                    ? cs.Attendances.FirstOrDefault(a => a.StudentId == leaderId)
                      ?? cs.Attendances.FirstOrDefault()
                    : cs.Attendances.FirstOrDefault();
            if (cs.Attendances != null)
            {
                attended += cs.Attendances.Count(a => a.Status is SessionAttendanceStatus.Present);
                absentOrLate += cs.Attendances.Count(a =>
                    a.Status is SessionAttendanceStatus.Absent or SessionAttendanceStatus.Late);
            }

            dto.Sessions.Add(new EnrollmentSessionItemDto
            {
                ScheduleId = cs.Id,
                SessionNumber = sessionNumber,
                Date = cs.Date,
                Title = ResolveSessionTitle(cs, sessionNumber, enrollment.EnrollmentRequest, enrollment.Course)
                    ?? slot?.LabelEn ?? slot?.LabelAr,
                StartTime = slot?.StartTime,
                EndTime = slot?.EndTime,
                DurationMinutes = duration,
                Status = cs.Status,
                CanStart = CanStartSessionUtc(
                    enrollment.EnrollmentStatus, cs.Status, slot, cs.Date, utcNow, _sessionSettings.EnforceJoinWindow),
                CanJoin = SessionJoinRules.CanJoinUtc(
                    enrollment.EnrollmentStatus,
                    cs.Status,
                    cs.Date,
                    slot?.StartTime,
                    slot?.EndTime,
                    utcNow,
                    _sessionSettings.EnforceJoinWindow),
                TeacherAttendanceStatus = cs.TeacherAttendanceStatus.ToString(),
                TeacherJoinedAt = cs.TeacherJoinedAt,
                UnitName = unitName,
                LessonName = lessonName,
                AttendanceStatus = primaryAttendance?.Status.ToString(),
                Rating = primaryAttendance?.Rating,
                TeacherNote = cs.TeacherNote ?? primaryAttendance?.Note,
                Units = units,
            });
        }

        dto.SessionsTotal = schedules.Count;
        dto.SessionsCompleted = schedules.Count(s => s.Status == ScheduleStatus.Completed);
        dto.SessionsAttended = attended;
        dto.SessionsAbsentOrLate = absentOrLate;
        dto.NextSessionAt = schedules
            .Where(s => s.Status is ScheduleStatus.Scheduled or ScheduleStatus.InProgress)
            .Select(s =>
            {
                var start = s.TeacherAvailability?.TimeSlot?.StartTime ?? TimeSpan.Zero;
                return s.Date.ToDateTime(TimeOnly.FromTimeSpan(start), DateTimeKind.Utc);
            })
            .Where(dt => dt >= utcNow)
            .OrderBy(dt => dt)
            .Cast<DateTime?>()
            .FirstOrDefault();

        return dto;
    }

    public async Task<(bool Ok, string Message, bool Forbidden)> RemindPaymentAsync(
        int userId,
        int enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            return (false, "Teacher profile not found.", false);

        var enrollment = await _enrollmentRepository.GetByIdWithCourseAsync(enrollmentId, cancellationToken);
        if (enrollment == null)
            return (false, "Enrollment not found.", false);

        var owns = enrollment.ApprovedByTeacherId == teacher.Id
                   || (enrollment.Course != null && enrollment.Course.TeacherId == teacher.Id);
        if (!owns)
            return (false, "This enrollment does not belong to you.", true);

        if (enrollment.EnrollmentStatus != EnrollmentStatus.PendingPayment)
            return (false, "Payment reminders are only available for pending-payment enrollments.", false);

        var pendingCount = enrollment.Participants.Count(p => p.PaymentStatus == PaymentStatus.Pending);
        _logger.LogInformation(
            "Teacher {TeacherId} requested payment reminder for enrollment {EnrollmentId} ({PendingCount} pending participant(s)).",
            teacher.Id, enrollment.Id, pendingCount);

        return (true, "Payment reminder recorded.", false);
    }

    public async Task<(TeacherEnrollmentInvoiceDto? Dto, string? Error, bool Forbidden)> GetInvoiceAsync(
        int userId,
        int enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            return (null, "Teacher profile not found.", false);

        var enrollment = await _enrollmentRepository.GetByIdWithCourseAsync(enrollmentId, cancellationToken);
        if (enrollment == null)
            return (null, "Enrollment not found.", false);

        var owns = enrollment.ApprovedByTeacherId == teacher.Id
                   || (enrollment.Course != null && enrollment.Course.TeacherId == teacher.Id);
        if (!owns)
            return (null, "This enrollment does not belong to you.", true);

        var invoiceNumber = await _enrollmentRepository.GetSucceededInvoiceNumberAsync(
            enrollment.Id, cancellationToken);

        return (new TeacherEnrollmentInvoiceDto
        {
            InvoiceNumber = invoiceNumber,
            DownloadUrl = null,
        }, null, false);
    }

    public async Task<(EnrollmentConversationDto? Dto, string? Error, bool Forbidden)> GetOrCreateConversationAsync(
        int userId,
        int enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            return (null, "Teacher profile not found.", false);

        var enrollment = await _enrollmentRepository.GetByIdWithCourseAsync(enrollmentId, cancellationToken);
        if (enrollment == null)
            return (null, "Enrollment not found.", false);

        var ownsCourse = enrollment.Course != null && enrollment.Course.TeacherId == teacher.Id;
        var isApprover = enrollment.ApprovedByTeacherId == teacher.Id;
        if (!ownsCourse && !isApprover)
            return (null, "NOT_A_PARTICIPANT", true);

        var studentUserId = ResolveStudentUserId(enrollment);
        if (studentUserId <= 0)
            return (null, "Enrollment has no student user to chat with.", false);

        var conv = await _conversationRepository.EnsureExistsAsync(
            enrollment.Id,
            teacher.Id,
            studentUserId,
            cancellationToken);

        var dto = await _conversationRepository.GetHeaderDtoAsync(
            conv.Id, EnrollmentConversationCaller.Teacher, cancellationToken);
        if (dto == null)
            return (null, "Conversation could not be loaded.", false);

        return (dto, null, false);
    }

    public async Task<(EnrollmentConversationMessagesPageDto? Page, bool Forbidden)> GetConversationMessagesAsync(
        int userId,
        int conversationId,
        string? cursor,
        string? direction,
        int take,
        CancellationToken cancellationToken = default)
    {
        var participant = await _conversationRepository.ResolveParticipantAsync(
            conversationId, userId, cancellationToken);
        if (participant == null)
            return (null, true);

        DateTime? cursorDt = null;
        if (!string.IsNullOrWhiteSpace(cursor)
            && DateTime.TryParse(cursor, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            cursorDt = parsed;
        }

        var older = !string.Equals(direction, "newer", StringComparison.OrdinalIgnoreCase);
        var page = await _conversationRepository.GetMessagesPageAsync(
            conversationId, cursorDt, take, older, cancellationToken);
        return (page, false);
    }

    public async Task<(EnrollmentConversationMessageDto? Dto, string? Error, bool Forbidden)> PostConversationMessageAsync(
        int userId,
        int conversationId,
        string? content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (null, "Message content is required.", false);

        var trimmed = content.Trim();
        if (trimmed.Length > 4000)
            return (null, "Message content is too long.", false);

        var participant = await _conversationRepository.ResolveParticipantAsync(
            conversationId, userId, cancellationToken);
        if (participant == null)
            return (null, "NOT_A_PARTICIPANT", true);

        var message = await _conversationRepository.AppendMessageAsync(
            conversationId,
            senderUserId: userId,
            EnrollmentMessageType.Text,
            trimmed,
            cancellationToken);

        try
        {
            var otherUserId = participant.CallerRole == EnrollmentConversationCaller.Teacher
                ? participant.StudentUserId
                : participant.TeacherUserId;

            if (otherUserId > 0)
            {
                var user = await _userManager.FindByIdAsync(otherUserId.ToString());
                if (user?.Email != null)
                {
                    await _rabbitMq.QueueEmailAsync(new EmailMessage
                    {
                        To = user.Email,
                        Subject = "رسالة جديدة على محادثة التسجيل",
                        Body = "وصلتك رسالة جديدة. افتح المحادثة لقراءتها والرد عليها.",
                        QueuedAt = DateTime.UtcNow
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to email the other party in enrollment conversation {ConversationId}.",
                conversationId);
        }

        var sender = await _userManager.FindByIdAsync(userId.ToString());
        var dto = new EnrollmentConversationMessageDto
        {
            Id = message.Id,
            Type = message.MessageType,
            SenderUserId = message.SenderUserId,
            SenderDisplayName = sender != null
                ? ((sender.FirstName ?? "") + " " + (sender.LastName ?? "")).Trim()
                : null,
            SenderRole = participant.CallerRole == EnrollmentConversationCaller.Teacher ? "Teacher" : "Student",
            Content = message.Content,
            SentAt = message.SentAt
        };

        return (dto, null, false);
    }

    public async Task<(bool Ok, bool Forbidden)> MarkConversationReadAsync(
        int userId,
        int conversationId,
        CancellationToken cancellationToken = default)
    {
        var participant = await _conversationRepository.ResolveParticipantAsync(
            conversationId, userId, cancellationToken);
        if (participant == null)
            return (false, true);

        await _conversationRepository.MarkReadAsync(conversationId, participant.CallerRole, cancellationToken);
        return (true, false);
    }

    private static int ResolveStudentUserId(Enrollment enrollment)
    {
        if (enrollment.OwnerUserId is int ownerId && ownerId > 0)
            return ownerId;

        if (enrollment.LeaderStudent?.UserId > 0)
            return enrollment.LeaderStudent.UserId;

        return enrollment.Participants
            .OrderBy(p => p.Id)
            .Select(p => p.Student?.UserId ?? 0)
            .FirstOrDefault(id => id > 0);
    }

    private static bool CanStartSessionUtc(
        EnrollmentStatus enrollmentStatus,
        ScheduleStatus scheduleStatus,
        TimeSlot? timeSlot,
        DateOnly sessionDate,
        DateTime utcNow,
        bool enforceJoinWindow = true)
    {
        if (enrollmentStatus != EnrollmentStatus.Active) return false;
        if (scheduleStatus is not (ScheduleStatus.Scheduled or ScheduleStatus.InProgress)) return false;
        if (timeSlot == null) return false;

        var start = TimeOnly.FromTimeSpan(timeSlot.StartTime);
        var end = TimeOnly.FromTimeSpan(timeSlot.EndTime);
        if (end <= start) return false;

        if (!enforceJoinWindow)
            return true;

        var startUtc = sessionDate.ToDateTime(start, DateTimeKind.Utc);
        var endUtc = sessionDate.ToDateTime(end, DateTimeKind.Utc);

        return utcNow >= startUtc && utcNow <= endUtc;
    }

    private static string? ResolveSessionTitle(
        CourseSchedule schedule,
        int sessionNumber,
        CourseEnrollmentRequest? request,
        Course? course)
    {
        var proposedTitle = request?.ProposedSessions?
            .FirstOrDefault(p => p.SessionNumber == sessionNumber)?.Title;
        if (!string.IsNullOrWhiteSpace(proposedTitle))
            return proposedTitle;

        var courseTitle = course?.Sessions?
            .FirstOrDefault(s => s.SessionNumber == sessionNumber)?.Title
            ?? schedule.CourseSession?.Title;
        return string.IsNullOrWhiteSpace(courseTitle) ? null : courseTitle;
    }
}
