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
    private readonly ICourseEnrollmentRepository _enrollmentRepository;
    private readonly PaymentSettings _paymentSettings;

    public GetTeacherEnrollmentByIdQueryHandler(
        ITeacherRepository teacherRepository,
        ICourseEnrollmentRepository enrollmentRepository,
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
            .Include(e => e.Course).ThenInclude(c => c.TeachingMode)
            .Include(e => e.Course).ThenInclude(c => c.SessionType)
            .Include(e => e.Course).ThenInclude(c => c.Sessions)
            .Include(e => e.Student).ThenInclude(s => s.User)
            .Include(e => e.EnrollmentRequest!).ThenInclude(r => r.ProposedSessions)
            .Include(e => e.EnrollmentRequest!).ThenInclude(r => r.SelectedSessionSlots)
            .Include(e => e.CourseSchedules)
                .ThenInclude(cs => cs.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .Include(e => e.CourseEnrollmentPayments)
                .ThenInclude(cep => cep.Payment)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (enrollment == null)
            return NotFound<TeacherEnrollmentDetailDto>("Enrollment not found.");

        if (enrollment.Course.TeacherId != teacher.Id)
            return NotFound<TeacherEnrollmentDetailDto>("Enrollment does not belong to your course.");

        var amountDue = enrollment.EnrollmentRequest?.EstimatedTotalPrice ?? 0m;
        var amountPaid = enrollment.CourseEnrollmentPayments
            .Where(cep => cep.Status == PaymentStatus.Succeeded && cep.Payment != null)
            .Sum(cep => cep.Payment.TotalAmount);

        var paymentStatus = (amountDue > 0m && amountPaid >= amountDue)
            ? PaymentStatus.Succeeded
            : PaymentStatus.Pending;

        var dto = new TeacherEnrollmentDetailDto
        {
            Id = enrollment.Id,
            CourseId = enrollment.CourseId,
            CourseTitle = enrollment.Course.Title,
            TeachingModeNameEn = enrollment.Course.TeachingMode?.NameEn,
            SessionTypeNameEn = enrollment.Course.SessionType?.NameEn,
            Student = new TeacherEnrollmentStudentDto
            {
                StudentId = enrollment.StudentId,
                StudentName = enrollment.Student?.User != null
                    ? (enrollment.Student.User.FirstName + " " + enrollment.Student.User.LastName).Trim()
                    : null,
                IsMinor = enrollment.Student?.IsMinor ?? false
            },
            EnrollmentStatus = enrollment.EnrollmentStatus,
            ApprovedAt = enrollment.ApprovedAt,
            ActivatedAt = enrollment.ActivatedAt,
            PaymentDeadline = enrollment.PaymentDeadline,
            AmountDue = amountDue,
            AmountPaid = amountPaid,
            PaymentStatus = paymentStatus,
            Currency = _paymentSettings.DefaultCurrency
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
