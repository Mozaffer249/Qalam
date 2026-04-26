using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class ScheduleGenerationService : IScheduleGenerationService
{
    private record SessionTemplate(int SessionNumber, int DurationMinutes, string? Title, string? Notes);

    public List<CourseSchedule> Generate(
        Course course,
        CourseEnrollmentRequest request,
        int? courseEnrollmentId,
        int? courseGroupEnrollmentId,
        DateOnly startDate)
    {
        if ((courseEnrollmentId.HasValue) == (courseGroupEnrollmentId.HasValue))
            throw new ArgumentException("Exactly one of courseEnrollmentId or courseGroupEnrollmentId must be provided.");

        var sessions = BuildSessionList(course, request);
        if (sessions.Count == 0)
            return new List<CourseSchedule>();

        var slots = request.SelectedAvailabilities
            .Where(sa => sa.TeacherAvailability != null)
            .OrderBy(sa => sa.TeacherAvailability.DayOfWeekId)
            .ThenBy(sa => sa.TeacherAvailability.TimeSlot != null ? sa.TeacherAvailability.TimeSlot.StartTime : TimeSpan.Zero)
            .Select(sa => sa.TeacherAvailability)
            .ToList();

        if (slots.Count == 0)
            throw new InvalidOperationException("Cannot generate schedules: enrollment request has no selected availabilities.");

        var schedules = new List<CourseSchedule>(sessions.Count);
        var sessionIndex = 0;
        var slotIndex = 0;
        var cursor = startDate;

        while (sessionIndex < sessions.Count)
        {
            var slot = slots[slotIndex];
            var nextDate = NextOnOrAfter(cursor, slot.DayOfWeekId);

            schedules.Add(new CourseSchedule
            {
                CourseEnrollmentId = courseEnrollmentId,
                CourseGroupEnrollmentId = courseGroupEnrollmentId,
                Date = nextDate,
                TeacherAvailabilityId = slot.Id,
                DurationMinutes = sessions[sessionIndex].DurationMinutes,
                TeachingModeId = course.TeachingModeId,
                LocationId = null, // TODO: resolve for in-person teaching mode
                Status = ScheduleStatus.Scheduled
            });

            sessionIndex++;
            slotIndex++;

            if (slotIndex >= slots.Count)
            {
                slotIndex = 0;
                cursor = nextDate.AddDays(1);
            }
            else
            {
                cursor = nextDate;
            }
        }

        return schedules;
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
