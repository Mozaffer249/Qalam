using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class CourseEnrollmentRepository : GenericRepositoryAsync<CourseEnrollment>, ICourseEnrollmentRepository
{
    private readonly ApplicationDBContext _context;

    public CourseEnrollmentRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<CourseEnrollment> GetByStudentIdQueryable(int studentId)
    {
        return _context.CourseEnrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Course)
                .ThenInclude(c => c.TeachingMode)
            .Include(e => e.Course)
                .ThenInclude(c => c.SessionType)
            .Include(e => e.ApprovedByTeacher)
                .ThenInclude(t => t.User)
            .OrderByDescending(e => e.ApprovedAt);
    }

    public async Task<List<CourseEnrollment>> GetExpiredPendingPaymentAsync(DateTime now, CancellationToken ct)
    {
        return await _context.CourseEnrollments
            .Where(e => e.EnrollmentStatus == EnrollmentStatus.PendingPayment
                     && e.PaymentDeadline != null
                     && e.PaymentDeadline < now)
            .ToListAsync(ct);
    }

    public async Task<CourseEnrollment?> GetByIdForPaymentAsync(int id, CancellationToken ct)
    {
        return await _context.CourseEnrollments
            .Include(e => e.Student).ThenInclude(s => s.User)
            .Include(e => e.Student).ThenInclude(s => s.Guardian)
            .Include(e => e.Course).ThenInclude(c => c.SessionType)
            .Include(e => e.Course).ThenInclude(c => c.TeachingMode)
            .Include(e => e.Course).ThenInclude(c => c.Sessions)
            .Include(e => e.EnrollmentRequest!).ThenInclude(r => r.SelectedAvailabilities)
                .ThenInclude(sa => sa.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .Include(e => e.EnrollmentRequest!).ThenInclude(r => r.SelectedAvailabilities)
                .ThenInclude(sa => sa.TeacherAvailability)
                    .ThenInclude(ta => ta.DayOfWeek)
            .Include(e => e.EnrollmentRequest!).ThenInclude(r => r.ProposedSessions)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }
}
