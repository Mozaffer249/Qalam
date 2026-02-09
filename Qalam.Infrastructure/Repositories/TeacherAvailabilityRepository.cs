using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherAvailabilityRepository : GenericRepositoryAsync<TeacherAvailability>, ITeacherAvailabilityRepository
{
    private readonly ApplicationDBContext _context;
    private readonly DbSet<TeacherAvailability> _teacherAvailability;
    private readonly DbSet<TeacherAvailabilityException> _teacherAvailabilityExceptions;

    public TeacherAvailabilityRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
        _teacherAvailability = context.Set<TeacherAvailability>();
        _teacherAvailabilityExceptions = context.Set<TeacherAvailabilityException>();
    }

    public async Task<List<TeacherAvailability>> GetTeacherAvailabilityAsync(int teacherId)
    {
        return await _teacherAvailability
            .AsNoTracking()
            .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
            .Include(ta => ta.DayOfWeek)
            .Include(ta => ta.TimeSlot)
            .OrderBy(ta => ta.DayOfWeek.OrderIndex)
                .ThenBy(ta => ta.TimeSlot.StartTime)
            .ToListAsync();
    }

    public async Task<bool> HasAnyAvailabilityAsync(int teacherId)
    {
        return await _teacherAvailability
            .AnyAsync(ta => ta.TeacherId == teacherId && ta.IsActive);
    }

    public async Task<List<TeacherAvailabilityException>> GetTeacherExceptionsAsync(int teacherId, DateOnly? fromDate = null, DateOnly? toDate = null)
    {
        var query = _teacherAvailabilityExceptions
            .AsNoTracking()
            .Where(tae => tae.TeacherId == teacherId);

        if (fromDate.HasValue)
            query = query.Where(tae => tae.Date >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(tae => tae.Date <= toDate.Value);

        return await query
            .Include(tae => tae.TimeSlot)
            .OrderBy(tae => tae.Date)
                .ThenBy(tae => tae.TimeSlot.StartTime)
            .ToListAsync();
    }

    public async Task SaveTeacherAvailabilityAsync(int teacherId, List<DayAvailabilityDto> daySchedules)
    {
        // Remove existing availability
        await RemoveAllTeacherAvailabilityAsync(teacherId);

        // Add new availability slots
        var availabilitySlots = new List<TeacherAvailability>();

        foreach (var daySchedule in daySchedules)
        {
            foreach (var timeSlotId in daySchedule.TimeSlotIds)
            {
                availabilitySlots.Add(new TeacherAvailability
                {
                    TeacherId = teacherId,
                    DayOfWeekId = daySchedule.DayOfWeekId,
                    TimeSlotId = timeSlotId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (availabilitySlots.Any())
        {
            await _teacherAvailability.AddRangeAsync(availabilitySlots);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveAllTeacherAvailabilityAsync(int teacherId)
    {
        var existingSlots = await _teacherAvailability
            .Where(ta => ta.TeacherId == teacherId)
            .ToListAsync();

        if (existingSlots.Any())
        {
            _teacherAvailability.RemoveRange(existingSlots);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<TeacherAvailabilityException?> AddExceptionAsync(int teacherId, AddAvailabilityExceptionDto dto)
    {
        var exception = new TeacherAvailabilityException
        {
            TeacherId = teacherId,
            Date = dto.Date,
            TimeSlotId = dto.TimeSlotId,
            ExceptionType = dto.ExceptionType,
            Reason = dto.Reason,
            CreatedAt = DateTime.UtcNow
        };

        await _teacherAvailabilityExceptions.AddAsync(exception);
        await _context.SaveChangesAsync();

        // Return with navigation properties loaded
        return await _teacherAvailabilityExceptions
            .AsNoTracking()
            .Where(e => e.Id == exception.Id)
            .Include(e => e.TimeSlot)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> RemoveExceptionAsync(int exceptionId, int teacherId)
    {
        var exception = await _teacherAvailabilityExceptions
            .FirstOrDefaultAsync(e => e.Id == exceptionId && e.TeacherId == teacherId);

        if (exception == null)
            return false;

        _teacherAvailabilityExceptions.Remove(exception);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExceptionExistsAsync(int teacherId, DateOnly date, int timeSlotId)
    {
        return await _teacherAvailabilityExceptions
            .AnyAsync(e => e.TeacherId == teacherId && e.Date == date && e.TimeSlotId == timeSlotId);
    }
}
