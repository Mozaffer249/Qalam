using Qalam.Data.DTOs;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Teaching;
using Qalam.Data.Results;

namespace Qalam.Service.Abstracts;

public interface ITeachingConfigurationService
{
    // Teaching Mode Operations
    IQueryable<TeachingMode> GetTeachingModesQueryable();
    IQueryable<TeachingMode> GetActiveTeachingModesQueryable();
    Task<TeachingMode> GetTeachingModeByIdAsync(int id);
    Task<TeachingMode> GetTeachingModeByCodeAsync(string code);
    Task<TeachingMode> CreateTeachingModeAsync(TeachingMode mode);
    Task<TeachingMode> UpdateTeachingModeAsync(TeachingMode mode);
    Task<bool> DeleteTeachingModeAsync(int id);
    Task<bool> ToggleTeachingModeStatusAsync(int id);

    // Session Type Operations
    IQueryable<SessionType> GetSessionTypesQueryable();
    IQueryable<SessionType> GetActiveSessionTypesQueryable();
    Task<SessionType> GetSessionTypeByIdAsync(int id);
    Task<SessionType> GetSessionTypeByCodeAsync(string code);
    Task<SessionType> CreateSessionTypeAsync(SessionType sessionType);
    Task<SessionType> UpdateSessionTypeAsync(SessionType sessionType);
    Task<bool> DeleteSessionTypeAsync(int id);
    Task<bool> ToggleSessionTypeStatusAsync(int id);

    // Time Slot Operations
    IQueryable<TimeSlot> GetTimeSlotsQueryable();
    IQueryable<TimeSlot> GetActiveTimeSlotsQueryable();
    IQueryable<TimeSlot> GetTimeSlotsByDayOfWeekQueryable(int dayOfWeek);
    Task<TimeSlot> GetTimeSlotByIdAsync(int id);
    Task<TimeSlot> CreateTimeSlotAsync(TimeSlot timeSlot);
    Task<TimeSlot> UpdateTimeSlotAsync(TimeSlot timeSlot);
    Task<bool> DeleteTimeSlotAsync(int id);
    Task<bool> ToggleTimeSlotStatusAsync(int id);

    // Day of Week Operations
    IQueryable<DayOfWeekMaster> GetDaysOfWeekQueryable();
    IQueryable<DayOfWeekMaster> GetActiveDaysOfWeekQueryable();
    Task<DayOfWeekMaster> GetDayOfWeekByIdAsync(int id);

    // Pagination
    Task<PaginatedResult<TeachingMode>> GetPaginatedTeachingModesAsync(
        int pageNumber, int pageSize, string? search = null);
    Task<PaginatedResult<SessionType>> GetPaginatedSessionTypesAsync(
        int pageNumber, int pageSize, string? search = null);
    Task<PaginatedResult<TimeSlot>> GetPaginatedTimeSlotsAsync(
        int pageNumber, int pageSize);
    Task<PaginatedResult<DayOfWeekDto>> GetPaginatedDaysOfWeekAsync(
        int pageNumber, int pageSize);

    // Validation
    Task<bool> IsTeachingModeCodeUniqueAsync(string code, int? excludeId = null);
    Task<bool> IsSessionTypeCodeUniqueAsync(string code, int? excludeId = null);
    Task<bool> IsTimeSlotOverlappingAsync(int dayOfWeek, TimeSpan startTime, TimeSpan endTime, int? excludeId = null);
}
