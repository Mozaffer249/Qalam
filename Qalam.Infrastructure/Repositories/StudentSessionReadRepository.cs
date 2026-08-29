using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class StudentSessionReadRepository : IStudentSessionReadRepository
{
    private readonly ApplicationDBContext _context;
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;

    public StudentSessionReadRepository(
        ApplicationDBContext context,
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository)
    {
        _context = context;
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
    }

    public async Task<List<StudentSessionListItemDto>> ListForStudentUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var studentIds = await ResolveOwnedStudentIdsAsync(userId);
        if (studentIds.Count == 0)
            return [];

        var schedules = await _context.CourseSchedules.AsNoTracking()
            .Include(s => s.TeacherAvailability)
                .ThenInclude(a => a!.TimeSlot)
            .Include(s => s.Enrollment)
            .Include(s => s.Attendances)
            .Include(s => s.Complaints)
            .Where(s => s.Enrollment.Participants.Any(p => studentIds.Contains(p.StudentId)))
            .OrderByDescending(s => s.Date)
            .ThenByDescending(s => s.Id)
            .Take(200)
            .ToListAsync(cancellationToken);

        return schedules.Select(s =>
        {
            var studentId = s.Enrollment.Participants
                .Select(p => p.StudentId)
                .FirstOrDefault(id => studentIds.Contains(id));
            var studentAtt = s.Attendances.FirstOrDefault(a => a.StudentId == studentId);
            var openComplaint = s.Complaints
                .Where(c => c.StudentId == studentId)
                .FirstOrDefault(c => SessionComplaintRules.IsBlockingStatus(c.Status));
            var (primary, hints) = SessionDisplayStatusHelper.Compute(
                s.Status,
                s.TeacherAttendanceStatus,
                studentAtt?.Status);
            return new StudentSessionListItemDto
            {
                ScheduleId = s.Id,
                EnrollmentId = s.EnrollmentId,
                SessionNumber = s.Id,
                Title = s.TeacherAvailability?.TimeSlot?.LabelEn ?? s.TeacherAvailability?.TimeSlot?.LabelAr,
                Date = s.Date,
                StartTime = s.TeacherAvailability?.TimeSlot?.StartTime,
                DurationMinutes = s.DurationMinutes,
                Status = s.Status.ToString(),
                DisplayStatus = primary,
                DisplayStatusHints = hints,
                HasOpenComplaint = openComplaint != null,
                CanFileComplaint = SessionDisplayStatusHelper.CanStudentFileComplaint(
                    s.Status,
                    s.TeacherAttendanceStatus,
                    openComplaint != null),
            };
        }).ToList();
    }

    private async Task<HashSet<int>> ResolveOwnedStudentIdsAsync(int userId)
    {
        var owned = new HashSet<int>();
        var own = await _studentRepository.GetByUserIdAsync(userId);
        if (own != null)
            owned.Add(own.Id);
        var guardian = await _guardianRepository.GetByUserIdAsync(userId);
        if (guardian != null)
        {
            var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);
            foreach (var c in children)
                owned.Add(c.Id);
        }
        return owned;
    }
}
