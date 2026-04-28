using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Availability.Queries.GetTeacherAvailabilityByRange;

public class GetTeacherAvailabilityByRangeQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherAvailabilityByRangeQuery, Response<TeacherAvailabilityByWeekdayRangeDto>>
{
    private const int DefaultRangeDays = 30;
    private const int MaxRangeDays = 90;

    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherAvailabilityRepository _availabilityRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;

    public GetTeacherAvailabilityByRangeQueryHandler(
        ITeacherRepository teacherRepository,
        ITeacherAvailabilityRepository availabilityRepository,
        ICourseScheduleRepository scheduleRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _availabilityRepository = availabilityRepository;
        _scheduleRepository = scheduleRepository;
    }

    public async Task<Response<TeacherAvailabilityByWeekdayRangeDto>> Handle(
        GetTeacherAvailabilityByRangeQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByIdAsync(request.TeacherId);
        if (teacher == null)
            return NotFound<TeacherAvailabilityByWeekdayRangeDto>("Teacher not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = request.FromDate ?? today;
        var toDate = request.ToDate ?? fromDate.AddDays(DefaultRangeDays);

        if (toDate < fromDate)
            return BadRequest<TeacherAvailabilityByWeekdayRangeDto>("ToDate must be on or after FromDate.");

        // Cap range to avoid pathological responses.
        var maxAllowed = fromDate.AddDays(MaxRangeDays);
        if (toDate > maxAllowed)
            toDate = maxAllowed;

        // Weekly template (active only).
        var weeklySlots = await _availabilityRepository.GetTableNoTracking()
            .Include(a => a.TimeSlot)
            .Include(a => a.DayOfWeek)
            .Where(a => a.TeacherId == request.TeacherId && a.IsActive)
            .ToListAsync(cancellationToken);

        if (weeklySlots.Count == 0)
        {
            return Success(entity: new TeacherAvailabilityByWeekdayRangeDto
            {
                TeacherId = request.TeacherId,
                FromDate = fromDate,
                ToDate = toDate,
                Weekdays = new List<AvailabilityWeekdayDto>()
            });
        }

        // Per-date blackouts.
        var exceptions = await _availabilityRepository.GetTeacherExceptionsAsync(request.TeacherId, fromDate, toDate);
        var blackoutSet = exceptions
            .Where(ex => ex.ExceptionType == AvailabilityExceptionType.Blocked)
            .Select(ex => (ex.Date, ex.TimeSlotId))
            .ToHashSet();

        // Existing scheduled bookings.
        var slotIds = weeklySlots.Select(s => s.Id).ToList();
        var bookedSet = await _scheduleRepository.GetScheduledSlotsAsync(fromDate, toDate, slotIds, cancellationToken);

        // Group weekly template by DayOfWeekId for O(1) lookup per date.
        var slotsByDayOfWeek = weeklySlots
            .GroupBy(s => s.DayOfWeekId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.TimeSlot != null ? s.TimeSlot.StartTime : TimeSpan.Zero).ToList());

        static int UiWeekdaySortKey(int dayOfWeekId) => dayOfWeekId == 7 ? 0 : dayOfWeekId; // Saturday first

        var weekdays = slotsByDayOfWeek
            .Select(kvp =>
            {
                var dayOfWeekId = kvp.Key;
                var dailySlots = kvp.Value;

                // Collect all dates in range matching this weekday
                var matchingDates = new List<DateOnly>();
                for (var date = fromDate; date <= toDate; date = date.AddDays(1))
                {
                    var dotNetDow = (int)date.DayOfWeek; // 0=Sunday … 6=Saturday
                    var currentDayOfWeekId = dotNetDow + 1;
                    if (currentDayOfWeekId == dayOfWeekId)
                        matchingDates.Add(date);
                }

                var slotDtos = dailySlots.Select(slot =>
                {
                    var dateStatuses = matchingDates.Select(date =>
                    {
                        AvailabilitySlotStatus status;
                        if (bookedSet.Contains((date, slot.Id)))
                            status = AvailabilitySlotStatus.Booked;
                        else if (blackoutSet.Contains((date, slot.TimeSlotId)))
                            status = AvailabilitySlotStatus.Blocked;
                        else
                            status = AvailabilitySlotStatus.Free;

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

        return Success(entity: new TeacherAvailabilityByWeekdayRangeDto
        {
            TeacherId = request.TeacherId,
            FromDate = fromDate,
            ToDate = toDate,
            Weekdays = weekdays
        });
    }
}
