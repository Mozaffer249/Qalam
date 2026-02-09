using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class CourseRepository : GenericRepositoryAsync<Course>, ICourseRepository
{
    private readonly ApplicationDBContext _context;

    public CourseRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Course?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Courses
            .Include(c => c.Teacher!)
            .ThenInclude(t => t.User)
            .Include(c => c.Domain)
            .Include(c => c.Subject)
            .Include(c => c.Curriculum)
            .Include(c => c.Level)
            .Include(c => c.Grade)
            .Include(c => c.TeachingMode)
            .Include(c => c.SessionType)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public IQueryable<Course> GetByTeacherIdQueryable(int teacherId)
    {
        return _context.Courses
            .AsNoTracking()
            .Where(c => c.TeacherId == teacherId)
            .OrderByDescending(c => c.CreatedAt);
    }

    public async Task<bool> HasEnrollmentsAsync(int courseId)
    {
        return await _context.CourseEnrollments.AnyAsync(e => e.CourseId == courseId);
    }
}
