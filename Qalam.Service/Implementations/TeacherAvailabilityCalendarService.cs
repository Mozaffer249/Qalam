using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherAvailabilityCalendarService : ITeacherAvailabilityCalendarService
{
    private readonly ITeacherAvailabilityRepository _availabilityRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;

    public TeacherAvailabilityCalendarService(
        ITeacherAvailabilityRepository availabilityRepository,
        ICourseScheduleRepository scheduleRepository)
    {
        _availabilityRepository = availabilityRepository;
        _scheduleRepository = scheduleRepository;
    }

    public async Task<TeacherAvailabilityByWeekdayRangeDto> BuildWeekdayRangeDtoAsync(
        int teacherId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var weeklySlots = await _availabilityRepository.GetTableNoTracking()
            .Include(a => a.TimeSlot)
            .Include(a => a.DayOfWeek)
            .Where(a => a.TeacherId == teacherId && a.IsActive)
            .ToListAsync(cancellationToken);

        if (weeklySlots.Count == 0)
        {
            return new TeacherAvailabilityByWeekdayRangeDto
            {
                TeacherId = teacherId,
                FromDate = fromDate,
                ToDate = toDate,
                Weekdays = new List<AvailabilityWeekdayDto>()
            };
        }

        var exceptions = await _availabilityRepository.GetTeacherExceptionsAsync(teacherId, fromDate, toDate);
        var blackoutSet = exceptions
            .Where(ex => ex.ExceptionType == AvailabilityExceptionType.Blocked)
            .Select(ex => (ex.Date, ex.TimeSlotId))
            .ToHashSet();

        var slotIds = weeklySlots.Select(s => s.Id).ToList();
        var bookedSet = await _scheduleRepository.GetScheduledSlotsAsync(fromDate, toDate, slotIds, cancellationToken);

        var slotsByDayOfWeek = weeklySlots
            .GroupBy(s => s.DayOfWeekId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.TimeSlot != null ? s.TimeSlot.StartTime : TimeSpan.Zero).ToList());

        static int UiWeekdaySortKey(int dayOfWeekId) => dayOfWeekId == 7 ? 0 : dayOfWeekId;

        var weekdays = slotsByDayOfWeek
            .Select(kvp =>
            {
                var dayOfWeekId = kvp.Key;
                var dailySlots = kvp.Value;

                var matchingDates = new List<DateOnly>();
                for (var date = fromDate; date <= toDate; date = date.AddDays(1))
                {
                    var dotNetDow = (int)date.DayOfWeek;
                    var currentDayOfWeekId = dotNetDow + 1;
                    if (currentDayOfWeekId == dayOfWeekId)
                        matchingDates.Add(date);
                }

                var slotDtos = dailySlots.Select(slot =>
                {
                    var dateStatuses = matchingDates.Select(date =>
                    {
                        var status = ResolveCellStatus(date, slot, blackoutSet, bookedSet);
                        return new AvailabilitySlotDateStatusDto
                        {
                            Date = date,
                            Status = status
                        };
                    }).ToList();

                    return new AvailabilitySlotByWeekdayDto
                    {
                        TeacherAvailabilityId = slot.Id,
                        TimeSlotId = slot.TimeSlotId,
                        StartTime = slot.TimeSlot?.StartTime ?? TimeSpan.Zero,
                        EndTime = slot.TimeSlot?.EndTime ?? TimeSpan.Zero,
                        DurationMinutes = slot.TimeSlot?.DurationMinutes ?? 0,
                        LabelEn = slot.TimeSlot?.LabelEn,
                        Dates = dateStatuses
                    };
                }).ToList();

                return new AvailabilityWeekdayDto
                {
                    DayOfWeekId = dayOfWeekId,
                    DayNameEn = dailySlots.FirstOrDefault()?.DayOfWeek?.NameEn ?? string.Empty,
                    Slots = slotDtos
                };
            })
            .OrderBy(w => UiWeekdaySortKey(w.DayOfWeekId))
            .ToList();

        return new TeacherAvailabilityByWeekdayRangeDto
        {
            TeacherId = teacherId,
            FromDate = fromDate,
            ToDate = toDate,
            Weekdays = weekdays
        };
    }

    public async Task<IReadOnlyDictionary<(DateOnly Date, int TeacherAvailabilityId), AvailabilitySlotStatus>> GetSlotStatusesAsync(
        int teacherId,
        IReadOnlyCollection<(DateOnly Date, int TeacherAvailabilityId)> slotDates,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<(DateOnly, int), AvailabilitySlotStatus>();
        if (slotDates.Count == 0)
            return result;

        var weeklySlots = await _availabilityRepository.GetTableNoTracking()
            .Include(a => a.TimeSlot)
            .Where(a => a.TeacherId == teacherId && a.IsActive)
            .ToListAsync(cancellationToken);

        var slotById = weeklySlots.ToDictionary(s => s.Id);
        var slotIds = weeklySlots.Select(s => s.Id).ToList();

        var dates = slotDates.Select(x => x.Date).ToList();
        var fromDate = dates.Min();
        var toDate = dates.Max();

        var exceptions = await _availabilityRepository.GetTeacherExceptionsAsync(teacherId, fromDate, toDate);
        var blackoutSet = exceptions
            .Where(ex => ex.ExceptionType == AvailabilityExceptionType.Blocked)
            .Select(ex => (ex.Date, ex.TimeSlotId))
            .ToHashSet();

        var bookedSet = await _scheduleRepository.GetScheduledSlotsAsync(fromDate, toDate, slotIds, cancellationToken);

        foreach (var pair in slotDates.Distinct())
        {
            if (!slotById.TryGetValue(pair.TeacherAvailabilityId, out var slot))
            {
                result[pair] = AvailabilitySlotStatus.Blocked;
                continue;
            }

            if (!DateMatchesDayOfWeek(pair.Date, slot.DayOfWeekId))
            {
                result[pair] = AvailabilitySlotStatus.Blocked;
                continue;
            }

            result[pair] = ResolveCellStatus(pair.Date, slot, blackoutSet, bookedSet);
        }

        return result;
    }

    internal static bool DateMatchesDayOfWeek(DateOnly date, int dayOfWeekId)
    {
        var dotNetDow = (int)date.DayOfWeek;
        var currentDayOfWeekId = dotNetDow + 1;
        return currentDayOfWeekId == dayOfWeekId;
    }

    private static AvailabilitySlotStatus ResolveCellStatus(
        DateOnly date,
        TeacherAvailability slot,
        HashSet<(DateOnly Date, int TimeSlotId)> blackoutSet,
        HashSet<(DateOnly Date, int TeacherAvailabilityId)> bookedSet)
    {
        if (bookedSet.Contains((date, slot.Id)))
            return AvailabilitySlotStatus.Booked;
        if (blackoutSet.Contains((date, slot.TimeSlotId)))
            return AvailabilitySlotStatus.Blocked;
        return AvailabilitySlotStatus.Free;
    }
}
