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

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetTeacherGroupEnrollmentById;

public class GetTeacherGroupEnrollmentByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherGroupEnrollmentByIdQuery, Response<TeacherGroupEnrollmentDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseGroupEnrollmentRepository _groupRepository;
    private readonly PaymentSettings _paymentSettings;

    public GetTeacherGroupEnrollmentByIdQueryHandler(
        ITeacherRepository teacherRepository,
        ICourseGroupEnrollmentRepository groupRepository,
        IOptions<PaymentSettings> paymentSettings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _groupRepository = groupRepository;
        _paymentSettings = paymentSettings.Value;
    }

    public async Task<Response<TeacherGroupEnrollmentDetailDto>> Handle(
        GetTeacherGroupEnrollmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherGroupEnrollmentDetailDto>("Teacher profile not found.");

        var group = await _groupRepository.GetTableNoTracking()
            .Include(g => g.Course).ThenInclude(c => c.TeachingMode)
            .Include(g => g.Course).ThenInclude(c => c.SessionType)
            .Include(g => g.Course).ThenInclude(c => c.Sessions)
            .Include(g => g.LeaderStudent).ThenInclude(s => s.User)
            .Include(g => g.Members).ThenInclude(m => m.Student).ThenInclude(s => s.User)
            .Include(g => g.Members).ThenInclude(m => m.GroupEnrollmentMemberPayments)
                .ThenInclude(gp => gp.Payment)
            .Include(g => g.EnrollmentRequest).ThenInclude(r => r.ProposedSessions)
            .Include(g => g.EnrollmentRequest).ThenInclude(r => r.SelectedSessionSlots)
            .Include(g => g.EnrollmentRequest).ThenInclude(r => r.GroupMembers)
            .Include(g => g.CourseSchedules)
                .ThenInclude(cs => cs.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (group == null)
            return NotFound<TeacherGroupEnrollmentDetailDto>("Group enrollment not found.");

        if (group.Course.TeacherId != teacher.Id)
            return NotFound<TeacherGroupEnrollmentDetailDto>("Group enrollment does not belong to your course.");

        var totalAmount = group.EnrollmentRequest?.EstimatedTotalPrice ?? 0m;
        var memberCount = group.Members.Count;
        var baseShare = memberCount > 0
            ? Math.Round(totalAmount / memberCount, 2, MidpointRounding.AwayFromZero)
            : 0m;

        // MemberType is on the originating request, not the enrollment's member row.
        var requestMemberTypeByStudentId = group.EnrollmentRequest?.GroupMembers
            .ToDictionary(rm => rm.StudentId, rm => rm.MemberType)
            ?? new Dictionary<int, GroupMemberType>();

        decimal amountPaid = 0m;
        var memberDtos = new List<TeacherGroupEnrollmentMemberDto>(memberCount);

        foreach (var m in group.Members)
        {
            var paid = m.GroupEnrollmentMemberPayments
                .Where(gp => gp.Status == PaymentStatus.Succeeded && gp.Payment != null)
                .Sum(gp => gp.Payment.TotalAmount);
            amountPaid += paid;

            memberDtos.Add(new TeacherGroupEnrollmentMemberDto
            {
                StudentId = m.StudentId,
                StudentName = m.Student?.User != null
                    ? (m.Student.User.FirstName + " " + m.Student.User.LastName).Trim()
                    : null,
                IsMinor = m.Student?.IsMinor ?? false,
                MemberType = requestMemberTypeByStudentId.TryGetValue(m.StudentId, out var mt) ? mt : GroupMemberType.Own,
                PaymentStatus = m.PaymentStatus,
                PaidAt = m.PaidAt,
                Share = paid > 0 ? paid : baseShare
            });
        }

        var dto = new TeacherGroupEnrollmentDetailDto
        {
            Id = group.Id,
            CourseId = group.CourseId,
            CourseTitle = group.Course.Title,
            TeachingModeNameEn = group.Course.TeachingMode?.NameEn,
            SessionTypeNameEn = group.Course.SessionType?.NameEn,
            LeaderStudentId = group.LeaderStudentId,
            LeaderStudentName = group.LeaderStudent?.User != null
                ? (group.LeaderStudent.User.FirstName + " " + group.LeaderStudent.User.LastName).Trim()
                : null,
            Status = group.Status,
            ActivatedAt = group.ActivatedAt,
            PaymentDeadline = group.PaymentDeadline,
            TotalAmount = totalAmount,
            AmountPaid = amountPaid,
            AmountRemaining = totalAmount - amountPaid,
            Currency = _paymentSettings.DefaultCurrency,
            Members = memberDtos
        };

        var utcNow = DateTime.UtcNow;
        var schedules = group.CourseSchedules
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
                Title = ResolveSessionTitle(cs, i + 1, group.EnrollmentRequest, group.Course)
                    ?? slot?.LabelEn ?? slot?.LabelAr,
                StartTime = slot?.StartTime,
                EndTime = slot?.EndTime,
                DurationMinutes = duration,
                Status = cs.Status,
                CanStart = CanStartSessionUtc(group.Status, cs.Status, slot, cs.Date, utcNow)
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
