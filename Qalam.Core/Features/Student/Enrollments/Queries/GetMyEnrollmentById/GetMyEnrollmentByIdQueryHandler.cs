using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Enrollments.Queries.GetMyEnrollmentById;

public class GetMyEnrollmentByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentByIdQuery, Response<EnrollmentDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMapper _mapper;

    public GetMyEnrollmentByIdQueryHandler(
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        IEnrollmentRepository enrollmentRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _enrollmentRepository = enrollmentRepository;
        _mapper = mapper;
    }

    public async Task<Response<EnrollmentDetailDto>> Handle(
        GetMyEnrollmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var ownedStudentIds = new HashSet<int>();
        var ownStudent = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (ownStudent != null)
            ownedStudentIds.Add(ownStudent.Id);

        var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
        if (guardian != null)
        {
            var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);
            foreach (var child in children)
                ownedStudentIds.Add(child.Id);
        }

        var enrollment = await _enrollmentRepository.GetTableNoTracking()
            .Include(e => e.Course)
                .ThenInclude(c => c!.TeachingMode)
            .Include(e => e.Course)
                .ThenInclude(c => c!.SessionType)
            .Include(e => e.Course)
                .ThenInclude(c => c!.Sessions)
            .Include(e => e.EnrollmentRequest!)
                .ThenInclude(r => r.ProposedSessions)
            .Include(e => e.EnrollmentRequest!)
                .ThenInclude(r => r.SelectedSessionSlots)
            .Include(e => e.SelectedSessionSlots)
            .Include(e => e.ApprovedByTeacher)
                .ThenInclude(t => t.User)
            .Include(e => e.LeaderStudent).ThenInclude(s => s!.User)
            .Include(e => e.Participants).ThenInclude(p => p.Student).ThenInclude(s => s.User)
            .Include(e => e.CourseSchedules)
                .ThenInclude(cs => cs.TeacherAvailability)
                    .ThenInclude(ta => ta!.TimeSlot)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (enrollment == null)
            return NotFound<EnrollmentDetailDto>("Enrollment not found.");

        var isOwner = enrollment.OwnerUserId == request.UserId
                      || enrollment.EnrollmentRequest?.RequestedByUserId == request.UserId;
        var isParticipant = enrollment.Participants.Any(p => ownedStudentIds.Contains(p.StudentId));
        if (!isOwner && !isParticipant)
            return NotFound<EnrollmentDetailDto>("Enrollment not found.");

        var dto = _mapper.Map<EnrollmentDetailDto>(enrollment);
        dto.Participants = enrollment.Participants
            .Select(p => _mapper.Map<EnrollmentParticipantDto>(p))
            .ToList();

        dto.IsOwner = isOwner;
        dto.AmountDue = enrollment.AmountDue;
        dto.PaymentDeadline = enrollment.PaymentDeadline;

        var now = DateTime.UtcNow;
        var deadlineOk = !enrollment.PaymentDeadline.HasValue
                         || enrollment.PaymentDeadline.Value >= now;
        var alreadyPaid = enrollment.PaidByUserId.HasValue
                          || enrollment.Participants.Any(p => p.PaymentStatus == PaymentStatus.Succeeded);
        var pendingParticipant = enrollment.Participants
            .FirstOrDefault(p => p.PaymentStatus == PaymentStatus.Pending);

        dto.CanPay = isOwner
                     && enrollment.EnrollmentStatus == EnrollmentStatus.PendingPayment
                     && deadlineOk
                     && !alreadyPaid
                     && enrollment.AmountDue > 0
                     && pendingParticipant != null;
        dto.PayParticipantId = dto.CanPay ? pendingParticipant!.Id : null;
        dto.CanCancel = isOwner
                        && enrollment.EnrollmentStatus == EnrollmentStatus.PendingPayment;

        var utcNow = DateTime.UtcNow;
        var schedules = enrollment.CourseSchedules != null
            ? enrollment.CourseSchedules
                .OrderBy(cs => cs.Date)
                .ThenBy(cs => cs.TeacherAvailability != null && cs.TeacherAvailability.TimeSlot != null
                    ? cs.TeacherAvailability.TimeSlot.StartTime
                    : TimeSpan.Zero)
                .ToList()
            : [];

        dto.Sessions = [];
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
                Title = ResolveSessionTitle(
                        cs, i + 1, enrollment.EnrollmentRequest, enrollment.SelectedSessionSlots, enrollment.Course)
                    ?? slot?.LabelEn ?? slot?.LabelAr,
                StartTime = slot != null ? slot.StartTime : null,
                EndTime = slot != null ? slot.EndTime : null,
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
        if (enrollmentStatus != EnrollmentStatus.Active)
            return false;
        if (scheduleStatus != ScheduleStatus.Scheduled)
            return false;
        if (timeSlot == null)
            return false;

        var start = TimeOnly.FromTimeSpan(timeSlot.StartTime);
        var end = TimeOnly.FromTimeSpan(timeSlot.EndTime);
        if (end <= start)
            return false;

        var startUtc = sessionDate.ToDateTime(start, DateTimeKind.Utc);
        var endUtc = sessionDate.ToDateTime(end, DateTimeKind.Utc);

        return utcNow >= startUtc && utcNow <= endUtc;
    }

    private static string? ResolveSessionTitle(
        CourseSchedule schedule,
        int ordinalSessionNumber,
        CourseEnrollmentRequest? request,
        ICollection<EnrollmentSelectedSessionSlot>? enrollmentSlots,
        Course? course)
    {
        var sessionNumber = ordinalSessionNumber;
        var slotMatch = request?.SelectedSessionSlots?
            .FirstOrDefault(ss =>
                ss.SessionDate == schedule.Date && ss.TeacherAvailabilityId == schedule.TeacherAvailabilityId);
        if (slotMatch != null)
            sessionNumber = slotMatch.SessionNumber;
        else
        {
            var enrollmentSlot = enrollmentSlots?
                .FirstOrDefault(ss =>
                    ss.SessionDate == schedule.Date && ss.TeacherAvailabilityId == schedule.TeacherAvailabilityId);
            if (enrollmentSlot != null)
                sessionNumber = enrollmentSlot.SessionNumber;
        }

        var proposedTitle = request?.ProposedSessions?
            .FirstOrDefault(p => p.SessionNumber == sessionNumber)?.Title;
        if (!string.IsNullOrWhiteSpace(proposedTitle))
            return proposedTitle;

        var courseTitle = course?.Sessions?
            .FirstOrDefault(s => s.SessionNumber == sessionNumber)?.Title;
        return string.IsNullOrWhiteSpace(courseTitle) ? null : courseTitle;
    }
}
