using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherAvailabilityRepository : IGenericRepositoryAsync<TeacherAvailability>
{
    /// <summary>
    /// Get all teacher availability slots for a teacher
    /// </summary>
    Task<List<TeacherAvailability>> GetTeacherAvailabilityAsync(int teacherId);

    /// <summary>
    /// Check if teacher has any availability slots (optimized - doesn't retrieve data)
    /// </summary>
    Task<bool> HasAnyAvailabilityAsync(int teacherId);

    /// <summary>
    /// Get teacher availability exceptions with optional date filtering
    /// </summary>
    Task<List<TeacherAvailabilityException>> GetTeacherExceptionsAsync(int teacherId, DateOnly? fromDate = null, DateOnly? toDate = null);

    /// <summary>
    /// Save teacher availability (replaces existing)
    /// </summary>
    Task SaveTeacherAvailabilityAsync(int teacherId, List<DayAvailabilityDto> daySchedules);

    /// <summary>
    /// Remove all availability slots for a teacher
    /// </summary>
    Task RemoveAllTeacherAvailabilityAsync(int teacherId);

    /// <summary>
    /// Add an availability exception (holiday or extra time)
    /// </summary>
    Task<TeacherAvailabilityException?> AddExceptionAsync(int teacherId, AddAvailabilityExceptionDto dto);

    /// <summary>
    /// Remove an availability exception
    /// </summary>
    Task<bool> RemoveExceptionAsync(int exceptionId, int teacherId);

    /// <summary>
    /// Check if a specific exception exists
    /// </summary>
    Task<bool> ExceptionExistsAsync(int teacherId, DateOnly date, int timeSlotId);
}
