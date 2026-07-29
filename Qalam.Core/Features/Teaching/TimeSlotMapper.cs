using Qalam.Data.DTOs.Teaching;
using Qalam.Data.Entity.Common;

namespace Qalam.Core.Features.Teaching;

internal static class TimeSlotMapper
{
    public static TimeSlotDto ToDto(TimeSlot entity) => new()
    {
        Id = entity.Id,
        StartTime = entity.StartTime,
        EndTime = entity.EndTime,
        DurationMinutes = entity.DurationMinutes,
        LabelAr = entity.LabelAr,
        LabelEn = entity.LabelEn,
        IsActive = entity.IsActive
    };
}
