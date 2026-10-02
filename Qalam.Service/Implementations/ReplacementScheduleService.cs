using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class ReplacementScheduleService : IReplacementScheduleService
{
    private readonly ApplicationDBContext _db;

    public ReplacementScheduleService(ApplicationDBContext db) => _db = db;

    public async Task<CourseSchedule> CreateAsync(
        CourseSchedule source,
        string note,
        DateOnly? date = null,
        int? teacherAvailabilityId = null,
        int? durationMinutes = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var replacement = new CourseSchedule
        {
            EnrollmentId = source.EnrollmentId,
            CourseSessionId = source.CourseSessionId,
            Date = date ?? (source.Date > today ? source.Date : today.AddDays(7)),
            TeacherAvailabilityId = teacherAvailabilityId ?? source.TeacherAvailabilityId,
            DurationMinutes = durationMinutes is > 0 ? durationMinutes.Value : source.DurationMinutes,
            TeachingModeId = source.TeachingModeId,
            LocationId = source.LocationId,
            Status = ScheduleStatus.Scheduled,
            TeacherNote = note.Length > 500 ? note[..500] : note,
            ReplacesScheduleId = source.Id,
            CreatedAt = DateTime.UtcNow,
        };

        _db.CourseSchedules.Add(replacement);
        await _db.SaveChangesAsync(cancellationToken);
        return replacement;
    }

    public async Task<CourseSchedule> RescheduleAsync(
        CourseSchedule original,
        int teacherId,
        DateOnly date,
        int teacherAvailabilityId,
        ScheduleCancellationReason reason,
        int? policyCaseId,
        string note,
        CancellationToken cancellationToken = default)
    {
        var slot = await _db.TeacherAvailabilities
            .Include(a => a.TimeSlot)
            .FirstOrDefaultAsync(a => a.Id == teacherAvailabilityId, cancellationToken);
        if (slot == null || !slot.IsActive || slot.TeacherId != teacherId)
            throw new InvalidOperationException("The selected time is not available for this teacher.");

        if (PlatformTime.ToUtc(date, slot.TimeSlot.StartTime) <= DateTime.UtcNow)
            throw new InvalidOperationException("The new time must be in the future.");

        var taken = await _db.CourseSchedules.AnyAsync(s => s.Date == date
                                                             && s.TeacherAvailabilityId == teacherAvailabilityId
                                                             && (s.Status == ScheduleStatus.Scheduled || s.Status == ScheduleStatus.InProgress),
            cancellationToken);
        if (taken)
            throw new InvalidOperationException("The selected time is already booked.");

        original.Status = ScheduleStatus.Rescheduled;
        original.CancellationReason = reason;
        original.PolicyCaseId = policyCaseId ?? original.PolicyCaseId;

        return await CreateAsync(original, note, date, slot.Id, slot.TimeSlot.ResolveDurationMinutes(), cancellationToken);
    }
}
