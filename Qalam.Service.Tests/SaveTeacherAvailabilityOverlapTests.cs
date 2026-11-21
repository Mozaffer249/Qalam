using Qalam.Core.Features.Teacher.Commands.SaveTeacherAvailability;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common;

namespace Qalam.Service.Tests;

/// <summary>
/// Covers the 30-min + 60-min overlap guard inside SaveTeacherAvailabilityCommandHandler
/// that keeps a teacher from offering overlapping weekly bands (otherwise the
/// (Date, TeacherAvailabilityId) conflict key would double-book the overlap).
/// </summary>
public class SaveTeacherAvailabilityOverlapTests
{
    private static TimeSlot Slot(int id, int startHour, int startMin, int endHour, int endMin)
        => new()
        {
            Id = id,
            StartTime = new TimeSpan(startHour, startMin, 0),
            EndTime = new TimeSpan(endHour, endMin, 0),
            DurationMinutes = (int)(new TimeSpan(endHour, endMin, 0) - new TimeSpan(startHour, startMin, 0)).TotalMinutes,
            IsActive = true,
        };

    private static readonly List<TimeSlot> Catalog =
    [
        Slot(1, 9, 0, 10, 0),   // 09:00–10:00  (60 min)
        Slot(2, 9, 0, 9, 30),   // 09:00–09:30  (30 min)
        Slot(3, 9, 30, 10, 0),  // 09:30–10:00  (30 min)
        Slot(4, 10, 0, 11, 0),  // 10:00–11:00  (60 min)
    ];

    [Fact]
    public void FindOverlap_NonOverlappingSlots_ReturnsNull()
    {
        var day = new DayAvailabilityDto
        {
            DayOfWeekId = 2,
            TimeSlotIds = [1, 4],  // 09:00–10:00 then 10:00–11:00
        };

        var result = SaveTeacherAvailabilityCommandHandler.FindOverlap([day], Catalog);

        Assert.Null(result);
    }

    [Fact]
    public void FindOverlap_TwoAdjacentHalfHours_ReturnsNull()
    {
        var day = new DayAvailabilityDto
        {
            DayOfWeekId = 2,
            TimeSlotIds = [2, 3],  // 09:00–09:30 + 09:30–10:00
        };

        var result = SaveTeacherAvailabilityCommandHandler.FindOverlap([day], Catalog);

        Assert.Null(result);
    }

    [Fact]
    public void FindOverlap_FullHourAndContainedHalfHour_ReturnsErrorMessage()
    {
        var day = new DayAvailabilityDto
        {
            DayOfWeekId = 2,
            TimeSlotIds = [1, 2],  // 09:00–10:00 overlaps with 09:00–09:30
        };

        var result = SaveTeacherAvailabilityCommandHandler.FindOverlap([day], Catalog);

        Assert.NotNull(result);
        Assert.Contains("overlap", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Day 2", result);
    }

    [Fact]
    public void FindOverlap_OverlapOnDifferentDays_IsAllowed()
    {
        var days = new[]
        {
            new DayAvailabilityDto { DayOfWeekId = 1, TimeSlotIds = [1] },  // 09-10 Sun
            new DayAvailabilityDto { DayOfWeekId = 2, TimeSlotIds = [2] },  // 09-09:30 Mon
        };

        var result = SaveTeacherAvailabilityCommandHandler.FindOverlap(days, Catalog);

        Assert.Null(result);
    }

    [Fact]
    public void FindOverlap_EmptyDay_ReturnsNull()
    {
        var day = new DayAvailabilityDto { DayOfWeekId = 2, TimeSlotIds = [] };

        var result = SaveTeacherAvailabilityCommandHandler.FindOverlap([day], Catalog);

        Assert.Null(result);
    }

    [Fact]
    public void FindOverlap_UnknownTimeSlotId_IsIgnored()
    {
        // 9999 is not in the catalog — the handler treats unknown ids as a no-op
        // and only validates what it can resolve. The command validator is the
        // guard against truly bad ids (>0 enforced there).
        var day = new DayAvailabilityDto { DayOfWeekId = 2, TimeSlotIds = [9999, 2] };

        var result = SaveTeacherAvailabilityCommandHandler.FindOverlap([day], Catalog);

        Assert.Null(result);
    }
}
