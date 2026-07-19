using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetTeacherEnrollmentById;

public class GetTeacherEnrollmentByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherEnrollmentByIdQuery, Response<TeacherEnrollmentDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly PaymentSettings _paymentSettings;

    public GetTeacherEnrollmentByIdQueryHandler(
        ITeacherRepository teacherRepository,
        IEnrollmentRepository enrollmentRepository,
        IOptions<PaymentSettings> paymentSettings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _enrollmentRepository = enrollmentRepository;
        _paymentSettings = paymentSettings.Value;
    }

    public async Task<Response<TeacherEnrollmentDetailDto>> Handle(
        GetTeacherEnrollmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherEnrollmentDetailDto>("Teacher profile not found.");

        var enrollment = await _enrollmentRepository.GetTableNoTracking()
            .Include(e => e.Course!).ThenInclude(c => c.TeachingMode)
            .Include(e => e.Course!).ThenInclude(c => c.SessionType)
            .Include(e => e.Course!).ThenInclude(c => c.Sessions)
            .Include(e => e.Course!).ThenInclude(c => c.TeacherSubject).ThenInclude(ts => ts.Subject)
            .Include(e => e.LeaderStudent).ThenInclude(s => s!.User)
            .Include(e => e.Participants).ThenInclude(p => p.Student).ThenInclude(s => s.User)
            .Include(e => e.EnrollmentRequest!).ThenInclude(r => r.ProposedSessions)
            .Include(e => e.EnrollmentRequest!).ThenInclude(r => r.SelectedSessionSlots)
            .Include(e => e.OpenSessionRequest!).ThenInclude(r => r.Subject)
            .Include(e => e.OpenSessionRequest!).ThenInclude(r => r.TeachingMode)
            .Include(e => e.CourseSchedules)
                .ThenInclude(cs => cs.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (enrollment == null)
            return NotFound<TeacherEnrollmentDetailDto>("Enrollment not found.");

        var ownsCourse = enrollment.Course != null && enrollment.Course.TeacherId == teacher.Id;
        var isApprover = enrollment.ApprovedByTeacherId == teacher.Id;
        if (!ownsCourse && !isApprover)
            return NotFound<TeacherEnrollmentDetailDto>("Enrollment does not belong to your course.");

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
            Participants = participants
        };

        var utcNow = DateTime.UtcNow;
        var schedules = enrollment.CourseSchedules
            .OrderBy(cs => cs.Date)
            .ThenBy(cs => cs.TeacherAvailability != null && cs.TeacherAvailability.TimeSlot != null
                ? cs.TeacherAvailability.TimeSlot.StartTime
                : TimeSpan.Zero)
            .ToList();

        for (var i = 0; i < schedules.Count; i++)
        {
            var cs = schedules[i];
            var slot = cs.TeacherAvailability?.TimeSlot;
            var duration = cs.DurationMinutes > 0
                ? cs.DurationMinutes
                : slot?.ResolveDurationMinutes() ?? 0;

            dto.Sessions.Add(new EnrollmentSessionItemDto
            {
                ScheduleId = cs.Id,
                Date = cs.Date,
                Title = ResolveSessionTitle(cs, i + 1, enrollment.EnrollmentRequest, enrollment.Course)
                    ?? slot?.LabelEn ?? slot?.LabelAr,
                StartTime = slot?.StartTime,
                EndTime = slot?.EndTime,
                DurationMinutes = duration,
                Status = cs.Status,
                CanStart = CanStartSessionUtc(enrollment.EnrollmentStatus, cs.Status, slot, cs.Date, utcNow)
            });
        }

        return Success(entity: dto);
    }

    private static bool CanStartSessionUtc(
        EnrollmentStatus enrollmentStatus,
        ScheduleStatus scheduleStatus,
        TimeSlot? timeSlot,
        DateOnly sessionDate,
        DateTime utcNow)
    {
        if (enrollmentStatus != EnrollmentStatus.Active) return false;
        if (scheduleStatus != ScheduleStatus.Scheduled) return false;
        if (timeSlot == null) return false;

        var start = TimeOnly.FromTimeSpan(timeSlot.StartTime);
        var end = TimeOnly.FromTimeSpan(timeSlot.EndTime);
        if (end <= start) return false;

        var startUtc = sessionDate.ToDateTime(start, DateTimeKind.Utc);
        var endUtc = sessionDate.ToDateTime(end, DateTimeKind.Utc);

        return utcNow >= startUtc && utcNow <= endUtc;
    }

    private static string? ResolveSessionTitle(
        CourseSchedule schedule,
        int ordinalSessionNumber,
        CourseEnrollmentRequest? request,
        Course? course)
    {
        var sessionNumber = ordinalSessionNumber;
        var slotMatch = request?.SelectedSessionSlots?
            .FirstOrDefault(ss =>
                ss.SessionDate == schedule.Date && ss.TeacherAvailabilityId == schedule.TeacherAvailabilityId);
        if (slotMatch != null)
            sessionNumber = slotMatch.SessionNumber;

        var proposedTitle = request?.ProposedSessions?
            .FirstOrDefault(p => p.SessionNumber == sessionNumber)?.Title;
        if (!string.IsNullOrWhiteSpace(proposedTitle))
            return proposedTitle;

        var courseTitle = course?.Sessions?
            .FirstOrDefault(s => s.SessionNumber == sessionNumber)?.Title;
        return string.IsNullOrWhiteSpace(courseTitle) ? null : courseTitle;
    }
}
