using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class EnrollmentRepository : GenericRepositoryAsync<Enrollment>, IEnrollmentRepository
{
    private readonly ApplicationDBContext _context;

    public EnrollmentRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<Enrollment> GetByStudentIdQueryable(int studentId)
    {
        return _context.Enrollments
            .AsNoTracking()
            .Where(e => e.Participants.Any(p => p.StudentId == studentId))
            .Include(e => e.Course)
                .ThenInclude(c => c.TeachingMode)
            .Include(e => e.Course)
                .ThenInclude(c => c.SessionType)
            .Include(e => e.Course)
                .ThenInclude(c => c.TeacherSubject)
                    .ThenInclude(ts => ts.Subject)
            .Include(e => e.Course)
                .ThenInclude(c => c.Sessions)
            .Include(e => e.ApprovedByTeacher)
                .ThenInclude(t => t.User)
            .Include(e => e.LeaderStudent)
                .ThenInclude(s => s.User)
            .Include(e => e.Participants)
            .OrderByDescending(e => e.ApprovedAt);
    }

    public async Task<List<Enrollment>> GetExpiredPendingPaymentAsync(DateTime now, CancellationToken ct)
    {
        return await _context.Enrollments
            .Where(e => e.EnrollmentStatus == EnrollmentStatus.PendingPayment
                     && e.PaymentDeadline != null
                     && e.PaymentDeadline < now)
            .Include(e => e.Participants)
            .ToListAsync(ct);
    }

    public async Task<Enrollment?> GetByIdForPaymentAsync(int id, CancellationToken ct)
    {
        return await _context.Enrollments
            .Include(e => e.Course).ThenInclude(c => c.SessionType)
            .Include(e => e.Course).ThenInclude(c => c.TeachingMode)
            .Include(e => e.Course).ThenInclude(c => c.Sessions)
            .Include(e => e.Participants).ThenInclude(p => p.Student).ThenInclude(s => s.User)
            .Include(e => e.Participants).ThenInclude(p => p.Student).ThenInclude(s => s.Guardian)
            .Include(e => e.EnrollmentRequest!).ThenInclude(r => r.SelectedAvailabilities)
                .ThenInclude(sa => sa.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .Include(e => e.EnrollmentRequest!).ThenInclude(r => r.SelectedAvailabilities)
                .ThenInclude(sa => sa.TeacherAvailability)
                    .ThenInclude(ta => ta.DayOfWeek)
            .Include(e => e.EnrollmentRequest!).ThenInclude(r => r.ProposedSessions)
            .Include(e => e.EnrollmentRequest!).ThenInclude(r => r.SelectedSessionSlots)
                .ThenInclude(ss => ss.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .Include(e => e.EnrollmentRequest!).ThenInclude(r => r.SelectedSessionSlots)
                .ThenInclude(ss => ss.TeacherAvailability)
                    .ThenInclude(ta => ta.DayOfWeek)
            .Include(e => e.SelectedSessionSlots)
                .ThenInclude(ss => ss.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .Include(e => e.SelectedSessionSlots)
                .ThenInclude(ss => ss.TeacherAvailability)
                    .ThenInclude(ta => ta.DayOfWeek)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<Enrollment?> GetByIdWithParticipantsAsync(int id, CancellationToken ct)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course).ThenInclude(c => c.SessionType)
            .Include(e => e.Course).ThenInclude(c => c.TeachingMode)
            .Include(e => e.ApprovedByTeacher).ThenInclude(t => t.User)
            .Include(e => e.LeaderStudent).ThenInclude(s => s!.User)
            .Include(e => e.Participants).ThenInclude(p => p.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }
}
