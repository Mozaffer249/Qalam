using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITimeSlotRepository : IGenericRepositoryAsync<TimeSlot>
{
    IQueryable<TimeSlot> GetTimeSlotsQueryable();
    IQueryable<TimeSlot> GetActiveTimeSlotsQueryable();
    IQueryable<TimeSlot> GetTimeSlotsByDayOfWeek(int dayOfWeek);
    Task<bool> IsTimeSlotOverlappingAsync(int dayOfWeek, TimeSpan startTime, TimeSpan endTime, int? excludeId = null);
}
