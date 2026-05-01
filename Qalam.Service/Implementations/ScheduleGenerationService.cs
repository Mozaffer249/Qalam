using Qalam.Data.Entity.Common;
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

    public ScheduleGenerationResult PreviewExplicit(
        Course course,
        CourseEnrollmentRequest request,
        IReadOnlyList<(DateOnly Date, int TeacherAvailabilityId)> selectionsInSessionOrder,
        IReadOnlyDictionary<int, TeacherAvailability> availabilityById,
        IReadOnlyCollection<TeacherAvailabilityException> blockedExceptions,
        IReadOnlySet<(DateOnly Date, int TeacherAvailabilityId)> existingScheduledSlots,
        DateOnly? hardEndDate)
    {
        var sessions = BuildSessionList(course, request);
        if (sessions.Count == 0 && course.IsFlexible && selectionsInSessionOrder.Count > 0)
            sessions = BuildFlexibleImplicitSessionTemplates(selectionsInSessionOrder, availabilityById);

        var emitted = new List<ProposedScheduleSlot>(sessions.Count);
        var conflicts = new List<ScheduleConflict>();

        if (sessions.Count == 0)
            return new ScheduleGenerationResult(emitted, conflicts, true);

        if (selectionsInSessionOrder.Count != sessions.Count)
            throw new ArgumentException("Explicit selections count must match course session count.");

        var blackoutSet = new HashSet<(DateOnly Date, int TimeSlotId)>();
        foreach (var ex in blockedExceptions)
        {
            if (ex.ExceptionType == AvailabilityExceptionType.Blocked)
                blackoutSet.Add((ex.Date, ex.TimeSlotId));
        }

        for (var i = 0; i < sessions.Count; i++)
        {
            var template = sessions[i];
            var (date, taId) = selectionsInSessionOrder[i];

            if (hardEndDate.HasValue && date > hardEndDate.Value)
                return new ScheduleGenerationResult(emitted, conflicts, FitsInWindow: false);

            if (!availabilityById.TryGetValue(taId, out var slot))
            {
                conflicts.Add(new ScheduleConflict(
                    template.SessionNumber,
                    date,
                    taId,
                    "Unknown or invalid teacher availability slot."));
                continue;
            }

            if (!ExplicitDateMatchesWeeklySlot(date, slot.DayOfWeekId))
            {
                conflicts.Add(new ScheduleConflict(
                    template.SessionNumber,
                    date,
                    taId,
                    $"Date {date:yyyy-MM-dd} does not match this weekly slot."));
                continue;
            }

            if (existingScheduledSlots.Contains((date, slot.Id)))
            {
                conflicts.Add(new ScheduleConflict(
                    template.SessionNumber,
                    date,
                    slot.Id,
                    $"Date {date:yyyy-MM-dd} on this availability is already booked."));
                continue;
            }

            if (blackoutSet.Contains((date, slot.TimeSlotId)))
            {
                conflicts.Add(new ScheduleConflict(
                    template.SessionNumber,
                    date,
                    slot.Id,
                    "This time is blocked on that date."));
                continue;
            }

            emitted.Add(new ProposedScheduleSlot(
                template.SessionNumber,
                date,
                slot.Id,
                template.DurationMinutes,
                template.Title,
                template.Notes));
        }

        return new ScheduleGenerationResult(emitted, conflicts, FitsInWindow: true);
    }

    /// <summary>
    /// Weekday rules aligned with the teacher availability calendar (DayOfWeekMaster id 1–7).
    /// </summary>
    private static bool ExplicitDateMatchesWeeklySlot(DateOnly date, int dayOfWeekId)
    {
        var dotNetDow = (int)date.DayOfWeek;
        var currentDayOfWeekId = dotNetDow + 1;
        return currentDayOfWeekId == dayOfWeekId;
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

        ScheduleGenerationResult result;
        if (request.SelectedSessionSlots != null && request.SelectedSessionSlots.Count > 0)
        {
            var ordered = request.SelectedSessionSlots.OrderBy(s => s.SessionNumber).ToList();
            var selections = ordered.Select(s => (s.SessionDate, s.TeacherAvailabilityId)).ToList();
            var availabilityById = new Dictionary<int, TeacherAvailability>();
            foreach (var row in ordered)
            {
                if (row.TeacherAvailability != null)
                    availabilityById[row.TeacherAvailabilityId] = row.TeacherAvailability;
            }
            foreach (var sa in request.SelectedAvailabilities)
            {
                if (sa.TeacherAvailability != null)
                    availabilityById[sa.TeacherAvailability.Id] = sa.TeacherAvailability;
            }

            result = PreviewExplicit(
                course,
                request,
                selections,
                availabilityById,
                blockedExceptions: Array.Empty<TeacherAvailabilityException>(),
                existingScheduledSlots: new HashSet<(DateOnly, int)>(),
                hardEndDate: null);
        }
        else
        {
            var slots = request.SelectedAvailabilities
                .Select(sa => sa.TeacherAvailability)
                .Where(ta => ta != null)
                .ToList();

            // Generate without external conflict info — caller is responsible for re-validating
            // against existing CourseSchedules before persisting (race-loser handling lives in the
            // pay handler). Blocked exceptions are NOT honored here because Generate is an
            // unconditional persistence path; if the caller wants blackout-aware generation they
            // should run Preview first and validate.
            result = Preview(
                course,
                request,
                slots,
                blockedExceptions: Array.Empty<TeacherAvailabilityException>(),
                existingScheduledSlots: new HashSet<(DateOnly, int)>(),
                effectiveStart: startDate,
                hardEndDate: null);
        }

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
    /// Flexible course with no stored <see cref="CourseEnrollmentRequest.ProposedSessions"/>: derive one template per explicit
    /// calendar pick using each slot's time band duration (same source as pricing in the enrollment handler).
    /// </summary>
    private static List<SessionTemplate> BuildFlexibleImplicitSessionTemplates(
        IReadOnlyList<(DateOnly Date, int TeacherAvailabilityId)> selectionsInSessionOrder,
        IReadOnlyDictionary<int, TeacherAvailability> availabilityById)
    {
        var list = new List<SessionTemplate>(selectionsInSessionOrder.Count);
        for (var i = 0; i < selectionsInSessionOrder.Count; i++)
        {
            var (_, taId) = selectionsInSessionOrder[i];
            var durationMinutes = 0;
            if (availabilityById.TryGetValue(taId, out var ta) && ta.TimeSlot != null)
                durationMinutes = ta.TimeSlot.ResolveDurationMinutes();

            list.Add(new SessionTemplate(i + 1, durationMinutes, null, null));
        }

        return list;
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
