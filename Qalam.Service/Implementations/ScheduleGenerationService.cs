using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Teacher;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class ScheduleGenerationService : IScheduleGenerationService
{
    private const int MaxBlackoutAdvances = 52;

    private record SessionTemplate(int SessionNumber, int DurationMinutes, string? Title, string? Notes);

    public ScheduleGenerationResult Preview(
        Course course,
        CourseEnrollmentRequest request,
        IReadOnlyList<TeacherAvailability> slots,
        IReadOnlyCollection<TeacherAvailabilityException> blockedExceptions,
        IReadOnlySet<(DateOnly Date, int TeacherAvailabilityId)> existingScheduledSlots,
        DateOnly effectiveStart,
        DateOnly? hardEndDate)
    {
        var sessions = BuildSessionList(course, request);
        var emitted = new List<ProposedScheduleSlot>(sessions.Count);
        var conflicts = new List<ScheduleConflict>();

        if (sessions.Count == 0)
            return new ScheduleGenerationResult(emitted, conflicts, true);

        if (slots.Count == 0)
            throw new InvalidOperationException("Cannot preview schedules: no selected availabilities provided.");

        // Index blackouts by (Date, TimeSlotId) for O(1) lookup.
        var blackoutSet = new HashSet<(DateOnly Date, int TimeSlotId)>();
        foreach (var ex in blockedExceptions)
        {
            if (ex.ExceptionType == AvailabilityExceptionType.Blocked)
                blackoutSet.Add((ex.Date, ex.TimeSlotId));
        }

        var slotIndex = 0;
        var cursor = effectiveStart;
        var sortedSlots = slots
            .OrderBy(s => s.DayOfWeekId)
            .ThenBy(s => s.TimeSlot != null ? s.TimeSlot.StartTime : TimeSpan.Zero)
            .ToList();

        for (var sessionIndex = 0; sessionIndex < sessions.Count; sessionIndex++)
        {
            var slot = sortedSlots[slotIndex];
            var nextDate = NextOnOrAfter(cursor, slot.DayOfWeekId);

            // Skip Blocked exceptions silently — advance one week, retry.
            var attempt = 0;
            while (blackoutSet.Contains((nextDate, slot.TimeSlotId)) && attempt < MaxBlackoutAdvances)
            {
                nextDate = nextDate.AddDays(7);
                attempt++;
            }

            // Out-of-window?
            if (hardEndDate.HasValue && nextDate > hardEndDate.Value)
            {
                return new ScheduleGenerationResult(emitted, conflicts, FitsInWindow: false);
            }

            // Hard conflict with an already-Scheduled row?
            if (existingScheduledSlots.Contains((nextDate, slot.Id)))
            {
                conflicts.Add(new ScheduleConflict(
                    sessions[sessionIndex].SessionNumber,
                    nextDate,
                    slot.Id,
                    $"Date {nextDate:yyyy-MM-dd} on this availability is already booked."));
                // Continue computing remaining sessions so the user sees ALL conflicts at once.
            }
            else
            {
                emitted.Add(new ProposedScheduleSlot(
                    sessions[sessionIndex].SessionNumber,
                    nextDate,
                    slot.Id,
                    sessions[sessionIndex].DurationMinutes,
                    sessions[sessionIndex].Title,
                    sessions[sessionIndex].Notes));
            }

            slotIndex++;
            if (slotIndex >= sortedSlots.Count)
            {
                slotIndex = 0;
                cursor = nextDate.AddDays(1);
            }
            else
            {
                cursor = nextDate;
            }
        }

        return new ScheduleGenerationResult(emitted, conflicts, FitsInWindow: true);
    }

    public List<CourseSchedule> Generate(
        Course course,
        CourseEnrollmentRequest request,
        int? courseEnrollmentId,
        int? courseGroupEnrollmentId,
        DateOnly startDate)
    {
        if (courseEnrollmentId.HasValue == courseGroupEnrollmentId.HasValue)
            throw new ArgumentException("Exactly one of courseEnrollmentId or courseGroupEnrollmentId must be provided.");

        var slots = request.SelectedAvailabilities
            .Select(sa => sa.TeacherAvailability)
            .Where(ta => ta != null)
            .ToList();

        // Generate without external conflict info — caller is responsible for re-validating
        // against existing CourseSchedules before persisting (race-loser handling lives in the
        // pay handler). Blocked exceptions are NOT honored here because Generate is an
        // unconditional persistence path; if the caller wants blackout-aware generation they
        // should run Preview first and validate.
        var result = Preview(
            course,
            request,
            slots,
            blockedExceptions: Array.Empty<TeacherAvailabilityException>(),
            existingScheduledSlots: new HashSet<(DateOnly, int)>(),
            effectiveStart: startDate,
            hardEndDate: null);

        return result.Slots
            .Select(s => new CourseSchedule
            {
                CourseEnrollmentId = courseEnrollmentId,
                CourseGroupEnrollmentId = courseGroupEnrollmentId,
                Date = s.Date,
                TeacherAvailabilityId = s.TeacherAvailabilityId,
                DurationMinutes = s.DurationMinutes,
                TeachingModeId = course.TeachingModeId,
                LocationId = null, // TODO: resolve for in-person teaching mode
                Status = ScheduleStatus.Scheduled
            })
            .ToList();
    }

    private static List<SessionTemplate> BuildSessionList(Course course, CourseEnrollmentRequest request)
    {
        if (course.IsFlexible)
        {
            return request.ProposedSessions
                .OrderBy(p => p.SessionNumber)
                .Select(p => new SessionTemplate(p.SessionNumber, p.DurationMinutes, p.Title, p.Notes))
                .ToList();
        }

        return course.Sessions
            .OrderBy(s => s.SessionNumber)
            .Select(s => new SessionTemplate(s.SessionNumber, s.DurationMinutes, s.Title, s.Notes))
            .ToList();
    }

    /// <summary>
    /// Advance the cursor to the next occurrence of the given DayOfWeekMaster.Id (1=Sunday … 7=Saturday).
    /// .NET's DayOfWeek is 0..6 with 0=Sunday, so DayOfWeekMaster.Id == ((int)DayOfWeek + 1).
    /// </summary>
    private static DateOnly NextOnOrAfter(DateOnly cursor, int dayOfWeekId)
    {
        var targetDotNet = (DayOfWeek)((dayOfWeekId - 1) % 7);
        var diff = ((int)targetDotNet - (int)cursor.DayOfWeek + 7) % 7;
        return cursor.AddDays(diff);
    }
}
