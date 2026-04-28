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
    IRequestHandler<GetTeacherAvailabilityByRangeQuery, Response<TeacherAvailabilityByRangeDto>>
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

    public async Task<Response<TeacherAvailabilityByRangeDto>> Handle(
        GetTeacherAvailabilityByRangeQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByIdAsync(request.TeacherId);
        if (teacher == null)
            return NotFound<TeacherAvailabilityByRangeDto>("Teacher not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = request.FromDate ?? today;
        var toDate = request.ToDate ?? fromDate.AddDays(DefaultRangeDays);

        if (toDate < fromDate)
            return BadRequest<TeacherAvailabilityByRangeDto>("ToDate must be on or after FromDate.");

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
            return Success(entity: new TeacherAvailabilityByRangeDto
            {
                TeacherId = request.TeacherId,
                FromDate = fromDate,
                ToDate = toDate,
                Days = new List<AvailabilityDayDto>()
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

        var days = new List<AvailabilityDayDto>();
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            var dotNetDow = (int)date.DayOfWeek;             // 0=Sunday … 6=Saturday
            var dayOfWeekId = dotNetDow + 1;                  // matches DayOfWeekMaster.Id (1=Sunday)

            if (!slotsByDayOfWeek.TryGetValue(dayOfWeekId, out var dailySlots))
                continue;

            var slotDtos = dailySlots.Select(slot =>
            {
                AvailabilitySlotStatus status;
                if (bookedSet.Contains((date, slot.Id)))
                    status = AvailabilitySlotStatus.Booked;
                else if (blackoutSet.Contains((date, slot.TimeSlotId)))
                    status = AvailabilitySlotStatus.Blocked;
                else
                    status = AvailabilitySlotStatus.Free;

                return new AvailabilitySlotByDateDto
                {
                    TeacherAvailabilityId = slot.Id,
                    TimeSlotId = slot.TimeSlotId,
                    StartTime = slot.TimeSlot?.StartTime ?? TimeSpan.Zero,
                    EndTime = slot.TimeSlot?.EndTime ?? TimeSpan.Zero,
                    DurationMinutes = slot.TimeSlot?.DurationMinutes ?? 0,
                    LabelEn = slot.TimeSlot?.LabelEn,
                    Status = status
                };
            }).ToList();

            days.Add(new AvailabilityDayDto
            {
                Date = date,
                DayOfWeekId = dayOfWeekId,
                DayNameEn = dailySlots[0].DayOfWeek?.NameEn ?? string.Empty,
                Slots = slotDtos
            });
        }

        return Success(entity: new TeacherAvailabilityByRangeDto
        {
            TeacherId = request.TeacherId,
            FromDate = fromDate,
            ToDate = toDate,
            Days = days
        });
    }
}
