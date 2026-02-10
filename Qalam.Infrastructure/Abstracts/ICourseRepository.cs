using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ICourseRepository : IGenericRepositoryAsync<Course>
{
    /// <summary>
    /// Gets a course by ID with all navigation properties loaded (Domain, Subject, Teacher, etc.)
    /// </summary>
    Task<Course?> GetByIdWithDetailsAsync(int courseId);
    
    /// <summary>
    /// Checks if a course has any enrollments (active or completed)
    /// </summary>
    Task<bool> HasEnrollmentsAsync(int courseId);
    
    /// <summary>
    /// Gets courses for a specific teacher with filtering and pagination
    /// </summary>
    IQueryable<Course> GetTeacherCoursesQueryable(int teacherId);
}
