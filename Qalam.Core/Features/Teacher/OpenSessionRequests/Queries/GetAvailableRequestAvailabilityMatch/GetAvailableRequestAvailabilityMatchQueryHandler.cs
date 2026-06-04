using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequestAvailabilityMatch;

public class GetAvailableRequestAvailabilityMatchQueryHandler : ResponseHandler,
    IRequestHandler<GetAvailableRequestAvailabilityMatchQuery, Response<List<SessionAvailabilityMatchDto>>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestTargetRepository _targetRepo;
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly ITeacherAvailabilityRepository _availabilityRepo;
    private readonly ICourseScheduleRepository _scheduleRepo;

    public GetAvailableRequestAvailabilityMatchQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestTargetRepository targetRepo,
        IOpenSessionRequestRepository requestRepo,
        ITeacherAvailabilityRepository availabilityRepo,
        ICourseScheduleRepository scheduleRepo) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _targetRepo = targetRepo;
        _requestRepo = requestRepo;
        _availabilityRepo = availabilityRepo;
        _scheduleRepo = scheduleRepo;
    }

    public async Task<Response<List<SessionAvailabilityMatchDto>>> Handle(
        GetAvailableRequestAvailabilityMatchQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<List<SessionAvailabilityMatchDto>>("Teacher account not active.");

        var target = await _targetRepo.GetByRequestAndTeacherAsync(request.RequestId, teacher.Id, cancellationToken);
        if (target == null)
            return Forbidden<List<SessionAvailabilityMatchDto>>("NOT_MATCHED");

        var slots = await _requestRepo.GetSessionScheduleSlotsAsync(request.RequestId, cancellationToken);
        if (slots.Count == 0)
            return Success(entity: new List<SessionAvailabilityMatchDto>());

        // Pass 1: build the (DayOfWeek, StartTime, EndTime) coverage set from the teacher's availability.
        var availabilityRows = await _availabilityRepo.GetTeacherAvailabilityAsync(teacher.Id);
        var availabilityKeys = availabilityRows
            .Where(a => a.IsActive && a.DayOfWeek != null && a.TimeSlot != null)
            .Select(a => (DayOfWeek: a.DayOfWeek!.OrderIndex, a.TimeSlot!.StartTime, a.TimeSlot!.EndTime))
            .ToHashSet();

        // Pass 2: fetch the teacher's booked Scheduled CourseSchedule slots for the date range.
        var withDate = slots.Where(s => s.PreferredDate.HasValue).ToList();
        var bookedSlots = new List<(DateOnly Date, TimeSpan Start, TimeSpan End)>();
        if (withDate.Count > 0)
        {
            var fromDate = withDate.Min(s => s.PreferredDate!.Value);
            var toDate = withDate.Max(s => s.PreferredDate!.Value);
            bookedSlots = await _scheduleRepo.GetTeacherBookedSlotsInRangeAsync(teacher.Id, fromDate, toDate, cancellationToken);
        }

        var result = new List<SessionAvailabilityMatchDto>(slots.Count);
        foreach (var slot in slots)
        {
            var dto = new SessionAvailabilityMatchDto
            {
                SessionId = slot.Id,
                SequenceNumber = slot.SequenceNumber,
                PreferredDate = slot.PreferredDate ?? default,
                TimeSlotId = slot.TimeSlotId ?? 0,
            };

            if (slot.PreferredDate == null || slot.TimeSlotStart == null || slot.TimeSlotEnd == null)
            {
                // Insufficient timing data to evaluate — treat as outside availability for safety.
                dto.Status = SessionAvailabilityStatus.OutsideAvailability;
                result.Add(dto);
                continue;
            }

            var dayOfWeekIndex = (int)slot.PreferredDate.Value.DayOfWeek;
            var inAvailability = availabilityKeys.Contains((dayOfWeekIndex, slot.TimeSlotStart.Value, slot.TimeSlotEnd.Value));
            if (!inAvailability)
            {
                dto.Status = SessionAvailabilityStatus.OutsideAvailability;
                result.Add(dto);
                continue;
            }

            // In availability — now check for conflicts on the same date with overlapping windows.
            var conflict = bookedSlots.FirstOrDefault(b =>
                b.Date == slot.PreferredDate.Value
                && b.Start < slot.TimeSlotEnd.Value
                && b.End > slot.TimeSlotStart.Value);

            if (conflict != default)
            {
                dto.Status = SessionAvailabilityStatus.Conflict;
                dto.ConflictWith = $"Booked {conflict.Date:yyyy-MM-dd} {conflict.Start:hh\\:mm}-{conflict.End:hh\\:mm}";
            }
            else
            {
                dto.Status = SessionAvailabilityStatus.Available;
            }
            result.Add(dto);
        }

        return Success(entity: result);
    }
}
