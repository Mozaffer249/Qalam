using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Teaching;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeachingConfigurationService : ITeachingConfigurationService
{
    private readonly ITeachingModeRepository _teachingModeRepository;
    private readonly ISessionTypeRepository _sessionTypeRepository;
    private readonly ITimeSlotRepository _timeSlotRepository;
    private readonly IDayOfWeekRepository _dayOfWeekRepository;

    public TeachingConfigurationService(
        ITeachingModeRepository teachingModeRepository,
        ISessionTypeRepository sessionTypeRepository,
        ITimeSlotRepository timeSlotRepository,
        IDayOfWeekRepository dayOfWeekRepository)
    {
        _teachingModeRepository = teachingModeRepository;
        _sessionTypeRepository = sessionTypeRepository;
        _timeSlotRepository = timeSlotRepository;
        _dayOfWeekRepository = dayOfWeekRepository;
    }

    #region Teaching Mode Operations

    public IQueryable<TeachingMode> GetTeachingModesQueryable()
    {
        return _teachingModeRepository.GetTeachingModesQueryable();
    }

    public IQueryable<TeachingMode> GetActiveTeachingModesQueryable()
    {
        return _teachingModeRepository.GetActiveTeachingModesQueryable();
    }

    public async Task<TeachingMode> GetTeachingModeByIdAsync(int id)
    {
        return await _teachingModeRepository.GetByIdAsync(id);
    }

    public async Task<TeachingMode> GetTeachingModeByCodeAsync(string code)
    {
        return await _teachingModeRepository.GetTeachingModeByCodeAsync(code);
    }

    public async Task<TeachingMode> CreateTeachingModeAsync(TeachingMode mode)
    {
        if (!await IsTeachingModeCodeUniqueAsync(mode.Code))
            throw new InvalidOperationException("Teaching mode code already exists");

        return await _teachingModeRepository.AddAsync(mode);
    }

    public async Task<TeachingMode> UpdateTeachingModeAsync(TeachingMode mode)
    {
        var existing = await _teachingModeRepository.GetByIdAsync(mode.Id);
        if (existing == null)
            throw new InvalidOperationException("Teaching mode not found");

        if (!await IsTeachingModeCodeUniqueAsync(mode.Code, mode.Id))
            throw new InvalidOperationException("Teaching mode code already exists");

        existing.NameAr = mode.NameAr;
        existing.NameEn = mode.NameEn;
        existing.Code = mode.Code;
        existing.DescriptionAr = mode.DescriptionAr;
        existing.DescriptionEn = mode.DescriptionEn;

        await _teachingModeRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteTeachingModeAsync(int id)
    {
        var mode = await _teachingModeRepository.GetByIdAsync(id);
        if (mode == null)
            return false;

        await _teachingModeRepository.DeleteAsync(mode);
        return true;
    }

    public async Task<bool> ToggleTeachingModeStatusAsync(int id)
    {
        // TeachingMode doesn't have IsActive - just return true
        return true;
    }

    #endregion

    #region Session Type Operations

    public IQueryable<SessionType> GetSessionTypesQueryable()
    {
        return _sessionTypeRepository.GetSessionTypesQueryable();
    }

    public IQueryable<SessionType> GetActiveSessionTypesQueryable()
    {
        return _sessionTypeRepository.GetActiveSessionTypesQueryable();
    }

    public async Task<SessionType> GetSessionTypeByIdAsync(int id)
    {
        return await _sessionTypeRepository.GetByIdAsync(id);
    }

    public async Task<SessionType> GetSessionTypeByCodeAsync(string code)
    {
        return await _sessionTypeRepository.GetSessionTypeByCodeAsync(code);
    }

    public async Task<SessionType> CreateSessionTypeAsync(SessionType sessionType)
    {
        if (!await IsSessionTypeCodeUniqueAsync(sessionType.Code))
            throw new InvalidOperationException("Session type code already exists");

        return await _sessionTypeRepository.AddAsync(sessionType);
    }

    public async Task<SessionType> UpdateSessionTypeAsync(SessionType sessionType)
    {
        var existing = await _sessionTypeRepository.GetByIdAsync(sessionType.Id);
        if (existing == null)
            throw new InvalidOperationException("Session type not found");

        if (!await IsSessionTypeCodeUniqueAsync(sessionType.Code, sessionType.Id))
            throw new InvalidOperationException("Session type code already exists");

        existing.NameAr = sessionType.NameAr;
        existing.NameEn = sessionType.NameEn;
        existing.Code = sessionType.Code;
        existing.DescriptionAr = sessionType.DescriptionAr;
        existing.DescriptionEn = sessionType.DescriptionEn;

        await _sessionTypeRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteSessionTypeAsync(int id)
    {
        var sessionType = await _sessionTypeRepository.GetByIdAsync(id);
        if (sessionType == null)
            return false;

        await _sessionTypeRepository.DeleteAsync(sessionType);
        return true;
    }

    public async Task<bool> ToggleSessionTypeStatusAsync(int id)
    {
        // SessionType doesn't have IsActive - just return true
        return true;
    }

    #endregion

    #region Time Slot Operations

    public IQueryable<TimeSlot> GetTimeSlotsQueryable()
    {
        return _timeSlotRepository.GetTimeSlotsQueryable();
    }

    public IQueryable<TimeSlot> GetActiveTimeSlotsQueryable()
    {
        return _timeSlotRepository.GetActiveTimeSlotsQueryable();
    }

    public IQueryable<TimeSlot> GetTimeSlotsByDayOfWeekQueryable(int dayOfWeek)
    {
        return _timeSlotRepository.GetTimeSlotsByDayOfWeek(dayOfWeek);
    }

    public async Task<TimeSlot> GetTimeSlotByIdAsync(int id)
    {
        return await _timeSlotRepository.GetByIdAsync(id);
    }

    public async Task<TimeSlot> CreateTimeSlotAsync(TimeSlot timeSlot)
    {
        if (await IsTimeSlotOverlappingAsync(0, timeSlot.StartTime, timeSlot.EndTime))
            throw new InvalidOperationException("Time slot overlaps with existing slot");

        timeSlot.CreatedAt = DateTime.UtcNow;
        timeSlot.IsActive = true;
        return await _timeSlotRepository.AddAsync(timeSlot);
    }

    public async Task<TimeSlot> UpdateTimeSlotAsync(TimeSlot timeSlot)
    {
        var existing = await _timeSlotRepository.GetByIdAsync(timeSlot.Id);
        if (existing == null)
            throw new InvalidOperationException("Time slot not found");

        if (await IsTimeSlotOverlappingAsync(0, timeSlot.StartTime, timeSlot.EndTime, timeSlot.Id))
            throw new InvalidOperationException("Time slot overlaps with existing slot");

        existing.LabelAr = timeSlot.LabelAr;
        existing.LabelEn = timeSlot.LabelEn;
        existing.StartTime = timeSlot.StartTime;
        existing.EndTime = timeSlot.EndTime;
        existing.DurationMinutes = timeSlot.DurationMinutes;
        existing.IsActive = timeSlot.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _timeSlotRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteTimeSlotAsync(int id)
    {
        var timeSlot = await _timeSlotRepository.GetByIdAsync(id);
        if (timeSlot == null)
            return false;

        await _timeSlotRepository.DeleteAsync(timeSlot);
        return true;
    }

    public async Task<bool> ToggleTimeSlotStatusAsync(int id)
    {
        var timeSlot = await _timeSlotRepository.GetByIdAsync(id);
        if (timeSlot == null)
            return false;

        timeSlot.IsActive = !timeSlot.IsActive;
        timeSlot.UpdatedAt = DateTime.UtcNow;
        await _timeSlotRepository.UpdateAsync(timeSlot);
        return true;
    }

    #endregion

    #region Day of Week Operations

    public IQueryable<DayOfWeekMaster> GetDaysOfWeekQueryable()
    {
        return _dayOfWeekRepository.GetDaysOfWeekQueryable();
    }

    public IQueryable<DayOfWeekMaster> GetActiveDaysOfWeekQueryable()
    {
        return _dayOfWeekRepository.GetActiveDaysOfWeekQueryable();
    }

    public async Task<DayOfWeekMaster> GetDayOfWeekByIdAsync(int id)
    {
        return await _dayOfWeekRepository.GetByIdAsync(id);
    }

    #endregion

    #region Pagination

    public async Task<PaginatedResult<TeachingMode>> GetPaginatedTeachingModesAsync(
        int pageNumber, int pageSize, string? search = null)
    {
        var query = _teachingModeRepository.GetTeachingModesQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(tm =>
                tm.NameAr.Contains(search) ||
                tm.NameEn.Contains(search) ||
                tm.Code.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(tm => tm.NameEn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<TeachingMode>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedResult<SessionType>> GetPaginatedSessionTypesAsync(
        int pageNumber, int pageSize, string? search = null)
    {
        var query = _sessionTypeRepository.GetSessionTypesQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(st =>
                st.NameAr.Contains(search) ||
                st.NameEn.Contains(search) ||
                st.Code.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(st => st.NameEn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<SessionType>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedResult<TimeSlot>> GetPaginatedTimeSlotsAsync(
        int pageNumber, int pageSize)
    {
        var query = _timeSlotRepository.GetTimeSlotsQueryable();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(ts => ts.StartTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<TimeSlot>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedResult<DayOfWeekDto>> GetPaginatedDaysOfWeekAsync(
        int pageNumber, int pageSize)
    {
        var query = _dayOfWeekRepository.GetDaysOfWeekQueryable();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(d => d.OrderIndex)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DayOfWeekDto
            {
                Id = d.Id,
                NameAr = d.NameAr,
                NameEn = d.NameEn,
                OrderIndex = d.OrderIndex
            })
            .ToListAsync();

        return new PaginatedResult<DayOfWeekDto>(items, totalCount, pageNumber, pageSize);
    }

    #endregion

    #region Validation

    public async Task<bool> IsTeachingModeCodeUniqueAsync(string code, int? excludeId = null)
    {
        return await _teachingModeRepository.IsTeachingModeCodeUniqueAsync(code, excludeId);
    }

    public async Task<bool> IsSessionTypeCodeUniqueAsync(string code, int? excludeId = null)
    {
        return await _sessionTypeRepository.IsSessionTypeCodeUniqueAsync(code, excludeId);
    }

    public async Task<bool> IsTimeSlotOverlappingAsync(int dayOfWeek, TimeSpan startTime, TimeSpan endTime, int? excludeId = null)
    {
        return await _timeSlotRepository.IsTimeSlotOverlappingAsync(dayOfWeek, startTime, endTime, excludeId);
    }

    #endregion
}
