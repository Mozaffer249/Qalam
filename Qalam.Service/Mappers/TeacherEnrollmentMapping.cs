using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;

namespace Qalam.Service.Mappers;

internal static class TeacherEnrollmentMapping
{
    public static TeacherEnrollmentSourceBadge ResolveSourceBadge(
        EnrollmentSource source,
        bool isFlexible,
        bool isDirected)
    {
        if (source == EnrollmentSource.SessionRequest)
            return isDirected
                ? TeacherEnrollmentSourceBadge.DirectedRequest
                : TeacherEnrollmentSourceBadge.PublishedRequest;

        return isFlexible
            ? TeacherEnrollmentSourceBadge.FlexibleCourse
            : TeacherEnrollmentSourceBadge.FixedCourse;
    }

    public static string BuildDisplayName(Enrollment enrollment)
    {
        if (enrollment.Kind == EnrollmentKind.Individual)
        {
            var only = enrollment.Participants.FirstOrDefault();
            var student = only?.Student;
            return student?.User != null
                ? (student.User.FirstName + " " + student.User.LastName).Trim()
                : $"Student #{only?.StudentId ?? 0}";
        }

        var leaderName = enrollment.LeaderStudent?.User != null
            ? (enrollment.LeaderStudent.User.FirstName + " " + enrollment.LeaderStudent.User.LastName).Trim()
            : $"Student #{enrollment.LeaderStudentId}";
        return $"Group of {enrollment.Participants.Count} — Leader: {leaderName}";
    }

    public static TeacherEnrollmentListItemDto ToListItem(
        Enrollment enrollment,
        string currency)
    {
        var course = enrollment.Course;
        var isFlexible = course?.IsFlexible ?? false;
        var isDirected = enrollment.Source == EnrollmentSource.SessionRequest
                         && enrollment.OpenSessionRequest?.TargetedTeacherId != null;
        var paidCount = enrollment.Participants.Count(p => p.PaymentStatus == PaymentStatus.Succeeded);
        var amountDue = enrollment.AmountDue > 0
            ? enrollment.AmountDue
            : enrollment.EnrollmentRequest?.EstimatedTotalPrice ?? 0m;
        var amountPaid = ResolveAmountPaid(enrollment, amountDue, paidCount);
        var schedules = enrollment.CourseSchedules ?? [];
        var sessionsTotal = schedules.Count;
        var sessionsCompleted = schedules.Count(s => s.Status == ScheduleStatus.Completed);
        var utcNow = DateTime.UtcNow;
        var next = schedules
            .Where(s => s.Status is ScheduleStatus.Scheduled or ScheduleStatus.InProgress)
            .Select(s =>
            {
                var start = s.TeacherAvailability?.TimeSlot?.StartTime ?? TimeSpan.Zero;
                return PlatformTime.ToUtc(s.Date, start);
            })
            .Where(dt => dt >= utcNow)
            .OrderBy(dt => dt)
            .Cast<DateTime?>()
            .FirstOrDefault();

        var attendances = schedules.SelectMany(s => s.Attendances ?? Enumerable.Empty<SessionAttendance>()).ToList();
        var attended = attendances.Count(a => a.Status is SessionAttendanceStatus.Present);
        var absentOrLate = attendances.Count(a =>
            a.Status is SessionAttendanceStatus.Absent or SessionAttendanceStatus.Late);

        string? cancelledByLabel = null;
        if (enrollment.CancelledAt.HasValue)
        {
            if (enrollment.CancelledByUserId.HasValue &&
                enrollment.OwnerUserId.HasValue &&
                enrollment.CancelledByUserId == enrollment.OwnerUserId)
            {
                cancelledByLabel = "Student";
            }
            else if (enrollment.CancelledByUserId == null)
            {
                cancelledByLabel = "System";
            }
        }

        return new TeacherEnrollmentListItemDto
        {
            Id = enrollment.Id,
            Kind = enrollment.Kind,
            CourseId = enrollment.CourseId ?? 0,
            CourseTitle = course?.Title
                          ?? enrollment.OpenSessionRequest?.Subject?.NameEn
                          ?? enrollment.OpenSessionRequest?.Subject?.NameAr
                          ?? string.Empty,
            DisplayName = BuildDisplayName(enrollment),
            EnrollmentStatus = enrollment.EnrollmentStatus,
            ApprovedAt = enrollment.ApprovedAt,
            ActivatedAt = enrollment.ActivatedAt,
            PaymentDeadline = enrollment.PaymentDeadline,
            ParticipantCount = enrollment.Participants.Count,
            PaidParticipantCount = paidCount,
            SessionsCount = sessionsTotal,
            Source = enrollment.Source,
            IsFlexible = isFlexible,
            IsDirected = isDirected,
            SourceBadge = ResolveSourceBadge(enrollment.Source, isFlexible, isDirected),
            SubjectNameEn = course?.TeacherSubject?.Subject?.NameEn
                            ?? enrollment.OpenSessionRequest?.Subject?.NameEn,
            SubjectNameAr = course?.TeacherSubject?.Subject?.NameAr
                            ?? enrollment.OpenSessionRequest?.Subject?.NameAr,
            TeachingModeNameEn = course?.TeachingMode?.NameEn
                                 ?? enrollment.OpenSessionRequest?.TeachingMode?.NameEn,
            SessionTypeNameEn = course?.SessionType?.NameEn,
            SessionsCompleted = sessionsCompleted,
            SessionsTotal = sessionsTotal,
            AmountDue = amountDue,
            AmountPaid = amountPaid,
            AmountRemaining = Math.Max(0, amountDue - amountPaid),
            Currency = currency,
            IsFreeTrial = enrollment.IsFreeTrial,
            NextSessionAt = next,
            SessionsAttended = attended,
            SessionsAbsentOrLate = absentOrLate,
            CancelledAt = enrollment.CancelledAt,
            CancelledByLabel = cancelledByLabel,
        };
    }

    public static decimal ResolveAmountPaid(
        Enrollment enrollment,
        decimal amountDue,
        int paidCount)
    {
        if (amountDue <= 0) return 0m;
        if (enrollment.PaidByUserId.HasValue) return amountDue;

        var participantCount = enrollment.Participants.Count;
        if (participantCount <= 0) return 0m;
        if (paidCount >= participantCount) return amountDue;

        var baseShare = Math.Round(amountDue / participantCount, 2, MidpointRounding.AwayFromZero);
        return baseShare * paidCount;
    }
}
