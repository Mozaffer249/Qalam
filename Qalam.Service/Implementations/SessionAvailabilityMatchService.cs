using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class SessionAvailabilityMatchService : ISessionAvailabilityMatchService
{
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly ITeacherAvailabilityRepository _availabilityRepo;
    private readonly ICourseScheduleRepository _scheduleRepo;

    public SessionAvailabilityMatchService(
        IOpenSessionRequestRepository requestRepo,
        ITeacherAvailabilityRepository availabilityRepo,
        ICourseScheduleRepository scheduleRepo)
    {
        _requestRepo = requestRepo;
        _availabilityRepo = availabilityRepo;
        _scheduleRepo = scheduleRepo;
    }

    public async Task<List<SessionAvailabilityMatchDto>> MatchAsync(
        int teacherId,
        int sessionRequestId,
        CancellationToken cancellationToken = default)
    {
        var slots = await _requestRepo.GetSessionScheduleSlotsAsync(sessionRequestId, cancellationToken);
        if (slots.Count == 0)
            return new List<SessionAvailabilityMatchDto>();

        var availabilityRows = await _availabilityRepo.GetTeacherAvailabilityAsync(teacherId);
        var availabilityKeys = availabilityRows
            .Where(a => a.IsActive && a.DayOfWeek != null && a.TimeSlot != null)
            .Select(a => (DayOfWeek: a.DayOfWeek!.OrderIndex, a.TimeSlot!.StartTime, a.TimeSlot!.EndTime))
            .ToHashSet();

        var withDate = slots.Where(s => s.PreferredDate.HasValue).ToList();
        var bookedSlots = new List<(DateOnly Date, TimeSpan Start, TimeSpan End)>();
        if (withDate.Count > 0)
        {
            var fromDate = withDate.Min(s => s.PreferredDate!.Value);
            var toDate = withDate.Max(s => s.PreferredDate!.Value);
            bookedSlots = await _scheduleRepo.GetTeacherBookedSlotsInRangeAsync(
                teacherId, fromDate, toDate, cancellationToken);
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
                dto.Status = SessionAvailabilityStatus.OutsideAvailability;
                result.Add(dto);
                continue;
            }

            var sessionStartUtc = PlatformTime.ToUtc(slot.PreferredDate.Value, slot.TimeSlotStart.Value);
            if (sessionStartUtc <= DateTime.UtcNow)
            {
                dto.Status = SessionAvailabilityStatus.Past;
                result.Add(dto);
                continue;
            }

            // DayOfWeekMaster.OrderIndex is 1=Sunday … 7=Saturday; .NET DayOfWeek is 0=Sunday … 6=Saturday.
            var dayOfWeekIndex = (int)slot.PreferredDate.Value.DayOfWeek + 1;
            var inAvailability = availabilityKeys.Contains(
                (dayOfWeekIndex, slot.TimeSlotStart.Value, slot.TimeSlotEnd.Value));
            if (!inAvailability)
            {
                dto.Status = SessionAvailabilityStatus.OutsideAvailability;
                result.Add(dto);
                continue;
            }

            var conflict = bookedSlots.FirstOrDefault(b =>
                b.Date == slot.PreferredDate.Value
                && b.Start < slot.TimeSlotEnd.Value
                && b.End > slot.TimeSlotStart.Value);

            if (conflict != default)
            {
                dto.Status = SessionAvailabilityStatus.Conflict;
                dto.ConflictWith =
                    $"Booked {conflict.Date:yyyy-MM-dd} {conflict.Start:hh\\:mm}-{conflict.End:hh\\:mm}";
            }
            else
            {
                dto.Status = SessionAvailabilityStatus.Available;
            }

            result.Add(dto);
        }

        return result;
    }
}
