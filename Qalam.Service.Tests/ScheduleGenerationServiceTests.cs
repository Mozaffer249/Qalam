using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Teacher;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class ScheduleGenerationServiceTests
{
    private readonly ScheduleGenerationService _sut = new();

    [Fact]
    public void PreviewExplicit_emits_slots_when_dates_match_weekly_template()
    {
        var course = new Course { IsFlexible = false, TeachingModeId = 1 };
        course.Sessions = new List<CourseSession>
        {
            new() { SessionNumber = 1, DurationMinutes = 60, Title = "S1" },
            new() { SessionNumber = 2, DurationMinutes = 60, Title = "S2" }
        };

        var request = new CourseEnrollmentRequest();

        var slot = new TeacherAvailability
        {
            Id = 5,
            DayOfWeekId = 1,
            TimeSlotId = 9,
            TimeSlot = new Qalam.Data.Entity.Common.TimeSlot { Id = 9, DurationMinutes = 60 }
        };

        var selections = new List<(DateOnly Date, int TeacherAvailabilityId)>
        {
            (new DateOnly(2026, 5, 3), 5),
            (new DateOnly(2026, 5, 10), 5)
        };

        var availabilityById = new Dictionary<int, TeacherAvailability> { [5] = slot };

        ScheduleGenerationResult result = _sut.PreviewExplicit(
            course,
            request,
            selections,
            availabilityById,
            Array.Empty<TeacherAvailabilityException>(),
            new HashSet<(DateOnly Date, int TeacherAvailabilityId)>(),
            hardEndDate: null);

        Assert.Empty(result.Conflicts);
        Assert.True(result.FitsInWindow);
        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(new DateOnly(2026, 5, 3), result.Slots[0].Date);
    }

    [Fact]
    public void PreviewExplicit_conflict_when_slot_already_booked()
    {
        var course = new Course { IsFlexible = false, TeachingModeId = 1 };
        course.Sessions = new List<CourseSession>
        {
            new() { SessionNumber = 1, DurationMinutes = 60, Title = "S1" }
        };

        var request = new CourseEnrollmentRequest();
        var slot = new TeacherAvailability
        {
            Id = 5,
            DayOfWeekId = 1,
            TimeSlotId = 9,
            TimeSlot = new Qalam.Data.Entity.Common.TimeSlot { Id = 9, DurationMinutes = 60 }
        };

        var date = new DateOnly(2026, 5, 3);
        var selections = new List<(DateOnly Date, int TeacherAvailabilityId)> { (date, 5) };
        var booked = new HashSet<(DateOnly Date, int TeacherAvailabilityId)> { (date, 5) };

        var result = _sut.PreviewExplicit(
            course,
            request,
            selections,
            new Dictionary<int, TeacherAvailability> { [5] = slot },
            Array.Empty<TeacherAvailabilityException>(),
            booked,
            hardEndDate: null);

        Assert.Single(result.Conflicts);
        Assert.Empty(result.Slots);
    }

    [Fact]
    public void PreviewExplicit_conflict_on_blocked_exception()
    {
        var course = new Course { IsFlexible = false, TeachingModeId = 1 };
        course.Sessions = new List<CourseSession>
        {
            new() { SessionNumber = 1, DurationMinutes = 60, Title = "S1" }
        };

        var request = new CourseEnrollmentRequest();
        var slot = new TeacherAvailability
        {
            Id = 5,
            DayOfWeekId = 1,
            TimeSlotId = 9,
            TimeSlot = new Qalam.Data.Entity.Common.TimeSlot { Id = 9, DurationMinutes = 60 }
        };

        var date = new DateOnly(2026, 5, 3);
        var selections = new List<(DateOnly Date, int TeacherAvailabilityId)> { (date, 5) };

        var blocked = new[]
        {
            new TeacherAvailabilityException
            {
                Date = date,
                TimeSlotId = 9,
                ExceptionType = AvailabilityExceptionType.Blocked
            }
        };

        var result = _sut.PreviewExplicit(
            course,
            request,
            selections,
            new Dictionary<int, TeacherAvailability> { [5] = slot },
            blocked,
            new HashSet<(DateOnly Date, int TeacherAvailabilityId)>(),
            hardEndDate: null);

        Assert.Single(result.Conflicts);
    }

    [Fact]
    public void PreviewExplicit_flexible_without_proposals_uses_time_slot_duration_per_selection()
    {
        var course = new Course { IsFlexible = true, TeachingModeId = 1 };

        var request = new CourseEnrollmentRequest { ProposedSessions = new List<CourseRequestProposedSession>() };

        var slot = new TeacherAvailability
        {
            Id = 5,
            DayOfWeekId = 1,
            TimeSlotId = 9,
            TimeSlot = new Qalam.Data.Entity.Common.TimeSlot { Id = 9, DurationMinutes = 45 }
        };

        var selections = new List<(DateOnly Date, int TeacherAvailabilityId)>
        {
            (new DateOnly(2026, 5, 3), 5),
            (new DateOnly(2026, 5, 10), 5)
        };

        var result = _sut.PreviewExplicit(
            course,
            request,
            selections,
            new Dictionary<int, TeacherAvailability> { [5] = slot },
            Array.Empty<TeacherAvailabilityException>(),
            new HashSet<(DateOnly Date, int TeacherAvailabilityId)>(),
            hardEndDate: null);

        Assert.Empty(result.Conflicts);
        Assert.Equal(2, result.Slots.Count);
        Assert.All(result.Slots, s => Assert.Equal(45, s.DurationMinutes));
    }

    [Fact]
    public void PreviewExplicit_flexible_implicit_derives_minutes_from_start_end_when_duration_is_zero()
    {
        var course = new Course { IsFlexible = true, TeachingModeId = 1 };
        var request = new CourseEnrollmentRequest { ProposedSessions = new List<CourseRequestProposedSession>() };

        var slot = new TeacherAvailability
        {
            Id = 5,
            DayOfWeekId = 1,
            TimeSlotId = 9,
            TimeSlot = new Qalam.Data.Entity.Common.TimeSlot
            {
                Id = 9,
                DurationMinutes = 0,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(10, 45, 0)
            }
        };

        var selections = new List<(DateOnly Date, int TeacherAvailabilityId)>
        {
            (new DateOnly(2026, 5, 3), 5)
        };

        var result = _sut.PreviewExplicit(
            course,
            request,
            selections,
            new Dictionary<int, TeacherAvailability> { [5] = slot },
            Array.Empty<TeacherAvailabilityException>(),
            new HashSet<(DateOnly Date, int TeacherAvailabilityId)>(),
            hardEndDate: null);

        Assert.Empty(result.Conflicts);
        Assert.Single(result.Slots);
        Assert.Equal(45, result.Slots[0].DurationMinutes);
    }
}
