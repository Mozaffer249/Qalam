using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class CourseGroupEnrollmentRepository : GenericRepositoryAsync<CourseGroupEnrollment>, ICourseGroupEnrollmentRepository
{
    private readonly ApplicationDBContext _context;

    public CourseGroupEnrollmentRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<CourseGroupEnrollment>> GetExpiredPendingPaymentAsync(DateTime now, CancellationToken ct)
    {
        return await _context.CourseGroupEnrollments
            .Where(e => e.Status == EnrollmentStatus.PendingPayment
                     && e.PaymentDeadline != null
                     && e.PaymentDeadline < now)
            .Include(e => e.Members)
            .ToListAsync(ct);
    }

    public async Task<CourseGroupEnrollment?> GetByIdForPaymentAsync(int id, CancellationToken ct)
    {
        return await _context.CourseGroupEnrollments
            .Include(e => e.Course).ThenInclude(c => c.SessionType)
            .Include(e => e.Course).ThenInclude(c => c.TeachingMode)
            .Include(e => e.Course).ThenInclude(c => c.Sessions)
            .Include(e => e.Members).ThenInclude(m => m.Student).ThenInclude(s => s.User)
            .Include(e => e.Members).ThenInclude(m => m.Student).ThenInclude(s => s.Guardian)
            .Include(e => e.EnrollmentRequest).ThenInclude(r => r.SelectedAvailabilities)
                .ThenInclude(sa => sa.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .Include(e => e.EnrollmentRequest).ThenInclude(r => r.SelectedAvailabilities)
                .ThenInclude(sa => sa.TeacherAvailability)
                    .ThenInclude(ta => ta.DayOfWeek)
            .Include(e => e.EnrollmentRequest).ThenInclude(r => r.ProposedSessions)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }
}
