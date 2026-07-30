using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;

namespace Qalam.Data.Helpers;

/// <summary>
/// Join-based attendance: Present/Late from real join; Absent only on complete for never-joined.
/// </summary>
public static class SessionAttendanceRules
{
    public static DateTime? ResolveStartUtc(CourseSchedule schedule)
    {
        var slot = schedule.TeacherAvailability?.TimeSlot;
        if (slot == null)
            return null;
        return PlatformTime.ToUtc(schedule.Date, slot.StartTime);
    }

    public static SessionAttendanceStatus ResolveJoinStatus(
        DateTime joinedAtUtc,
        DateTime startUtc,
        int lateGraceMinutes)
    {
        var joined = NormalizeUtc(joinedAtUtc);
        var start = NormalizeUtc(startUtc);
        var threshold = start.AddMinutes(Math.Max(0, lateGraceMinutes));
        return joined <= threshold
            ? SessionAttendanceStatus.Present
            : SessionAttendanceStatus.Late;
    }

    public static int? ComputeLateMinutes(DateTime? joinedAt, DateTime? startUtc)
    {
        if (joinedAt == null || startUtc == null)
            return null;

        var joined = NormalizeUtc(joinedAt.Value);
        var start = NormalizeUtc(startUtc.Value);
        if (joined <= start)
            return null;

        return (int)Math.Round((joined - start).TotalMinutes);
    }

    /// <summary>
    /// Marks student Present/Late from an authenticated Join or best-effort webhook.
    /// Does not overwrite Excused. Sets JoinedAt once.
    /// </summary>
    public static void ApplyStudentJoin(
        SessionAttendance attendance,
        DateTime joinedAtUtc,
        DateTime? startUtc,
        int lateGraceMinutes)
    {
        if (attendance.Status == SessionAttendanceStatus.Excused)
            return;

        attendance.JoinedAt ??= NormalizeUtc(joinedAtUtc);
        attendance.IsAutoResolved = false;

        if (startUtc.HasValue)
        {
            attendance.Status = ResolveJoinStatus(
                attendance.JoinedAt.Value,
                startUtc.Value,
                lateGraceMinutes);
        }
        else
        {
            attendance.Status = SessionAttendanceStatus.Present;
        }
    }

    /// <summary>
    /// Marks teacher Present/Late from Join or webhook. Does not overwrite Excused.
    /// </summary>
    public static void ApplyTeacherJoin(
        CourseSchedule schedule,
        DateTime joinedAtUtc,
        DateTime? startUtc,
        int lateGraceMinutes)
    {
        if (schedule.TeacherAttendanceStatus == SessionAttendanceStatus.Excused)
            return;

        schedule.TeacherJoinedAt ??= NormalizeUtc(joinedAtUtc);

        if (startUtc.HasValue)
        {
            schedule.TeacherAttendanceStatus = ResolveJoinStatus(
                schedule.TeacherJoinedAt.Value,
                startUtc.Value,
                lateGraceMinutes);
        }
        else
        {
            schedule.TeacherAttendanceStatus = SessionAttendanceStatus.Present;
        }
    }

    /// <summary>
    /// On session complete: Pending + never joined → Absent. Never invent Present.
    /// Leaves Present/Late/Excused (and any Pending that somehow has JoinedAt) alone.
    /// </summary>
    public static void AutoResolveMissingAttendance(CourseSchedule schedule)
    {
        if (schedule.TeacherAttendanceStatus == SessionAttendanceStatus.Pending
            && schedule.TeacherJoinedAt == null)
        {
            schedule.TeacherAttendanceStatus = SessionAttendanceStatus.Absent;
        }

        var participants = schedule.Enrollment?.Participants;
        if (participants == null || participants.Count == 0)
            return;

        var byStudent = schedule.Attendances
            .GroupBy(a => a.StudentId)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var participant in participants)
        {
            if (byStudent.TryGetValue(participant.StudentId, out var existing))
            {
                if (existing.Status == SessionAttendanceStatus.Pending
                    && existing.JoinedAt == null)
                {
                    existing.Status = SessionAttendanceStatus.Absent;
                    existing.IsAutoResolved = true;
                }

                continue;
            }

            schedule.Attendances.Add(new SessionAttendance
            {
                CourseScheduleId = schedule.Id,
                StudentId = participant.StudentId,
                Status = SessionAttendanceStatus.Absent,
                IsAutoResolved = true,
            });
        }
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
}
