using Qalam.Data.DTOs.Teacher;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Computes slot availability for a teacher using the weekly template, blackout exceptions,
/// and existing course schedules — same rules as GET teacher availability by date range.
/// </summary>
public interface ITeacherAvailabilityCalendarService
{
    /// <summary>
    /// Builds the weekday-grouped calendar view for the student availability API.
    /// </summary>
    Task<TeacherAvailabilityByWeekdayRangeDto> BuildWeekdayRangeDtoAsync(
        int teacherId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns status for each concrete (date, teacherAvailabilityId) pair (e.g. enrollment picks).
    /// Unknown or inactive availability ids are reported as <see cref="AvailabilitySlotStatus.Blocked"/>.
    /// </summary>
    Task<IReadOnlyDictionary<(DateOnly Date, int TeacherAvailabilityId), AvailabilitySlotStatus>> GetSlotStatusesAsync(
        int teacherId,
        IReadOnlyCollection<(DateOnly Date, int TeacherAvailabilityId)> slotDates,
        CancellationToken cancellationToken = default);
}
