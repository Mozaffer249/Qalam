using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class CourseEnrollmentRequestRepository : GenericRepositoryAsync<CourseEnrollmentRequest>, ICourseEnrollmentRequestRepository
{
    private readonly ApplicationDBContext _context;

    public CourseEnrollmentRequestRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<CourseEnrollmentRequest> GetByStudentIdQueryable(int studentId)
    {
        return _context.CourseEnrollmentRequests
            .AsNoTracking()
            .Where(r => r.RequestedByStudentId == studentId)
            .Include(r => r.Course)
                .ThenInclude(c => c.TeachingMode)
            .Include(r => r.Course)
                .ThenInclude(c => c.SessionType)
            .OrderByDescending(r => r.CreatedAt);
    }
}
