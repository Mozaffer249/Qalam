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
            .ToListAsync(ct);
    }
}
