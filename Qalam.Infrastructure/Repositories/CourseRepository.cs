using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class CourseRepository : GenericRepositoryAsync<Course>, ICourseRepository
{
    private readonly DbSet<Course> _courses;
    private readonly ApplicationDBContext _context;

    public CourseRepository(ApplicationDBContext context) : base(context)
    {
        _courses = context.Set<Course>();
        _context = context;
    }

    public async Task<Course?> GetByIdWithDetailsAsync(int courseId)
    {
        return await _courses
            .Include(c => c.TeacherSubject)
                .ThenInclude(ts => ts.Subject)
                    .ThenInclude(s => s.Domain)
            .Include(c => c.TeacherSubject.Curriculum)
            .Include(c => c.TeacherSubject.Level)
            .Include(c => c.TeacherSubject.Grade)
            .Include(c => c.TeachingMode)
            .Include(c => c.SessionType)
            .Include(c => c.Teacher)
                .ThenInclude(t => t.User)
            .Include(c => c.CourseEnrollments.Where(e => e.EnrollmentStatus == EnrollmentStatus.Active))
            .FirstOrDefaultAsync(c => c.Id == courseId);
    }

    public async Task<bool> HasEnrollmentsAsync(int courseId)
    {
        return await _context.Set<CourseEnrollment>()
            .AnyAsync(e => e.CourseId == courseId && 
                          (e.EnrollmentStatus == EnrollmentStatus.Active || 
                           e.EnrollmentStatus == EnrollmentStatus.Completed));
    }

    public IQueryable<Course> GetTeacherCoursesQueryable(int teacherId)
    {
        return _courses
            .Include(c => c.TeacherSubject)
                .ThenInclude(ts => ts.Subject)
                    .ThenInclude(s => s.Domain)
            .Include(c => c.TeacherSubject.Curriculum)
            .Include(c => c.TeacherSubject.Level)
            .Include(c => c.TeacherSubject.Grade)
            .Include(c => c.TeachingMode)
            .Include(c => c.SessionType)
            .Include(c => c.CourseEnrollments.Where(e => e.EnrollmentStatus == EnrollmentStatus.Active))
            .Where(c => c.TeacherId == teacherId)
            .OrderByDescending(c => c.CreatedAt);
    }

    public IQueryable<Course> GetPublishedCoursesQueryable()
    {
        return _courses
            .AsNoTracking()
            .Where(c => c.Status == CourseStatus.Published && c.IsActive)
            .Include(c => c.TeacherSubject)
                .ThenInclude(ts => ts.Subject)
                    .ThenInclude(s => s.Domain)
            .Include(c => c.TeacherSubject.Curriculum)
            .Include(c => c.TeacherSubject.Level)
            .Include(c => c.TeacherSubject.Grade)
            .Include(c => c.TeachingMode)
            .Include(c => c.SessionType)
            .Include(c => c.Teacher)
                .ThenInclude(t => t.User)
            .Include(c => c.CourseEnrollments.Where(e => e.EnrollmentStatus == EnrollmentStatus.Active))
            .OrderByDescending(c => c.CreatedAt);
    }
}
