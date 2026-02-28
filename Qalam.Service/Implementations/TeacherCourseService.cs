using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Mappers;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherCourseService : ITeacherCourseService
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly ITeachingModeRepository _teachingModeRepository;
    private readonly ISessionTypeRepository _sessionTypeRepository;

    public TeacherCourseService(
        ITeacherRepository teacherRepository,
        ICourseRepository courseRepository,
        ITeacherSubjectRepository teacherSubjectRepository,
        ITeachingModeRepository teachingModeRepository,
        ISessionTypeRepository sessionTypeRepository)
    {
        _teacherRepository = teacherRepository;
        _courseRepository = courseRepository;
        _teacherSubjectRepository = teacherSubjectRepository;
        _teachingModeRepository = teachingModeRepository;
        _sessionTypeRepository = sessionTypeRepository;
    }

    public async Task<CourseDetailDto?> GetCourseByIdForTeacherAsync(int userId, int courseId, CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            return null;

        var course = await _courseRepository.GetByIdWithDetailsAsync(courseId);
        if (course == null || course.TeacherId != teacher.Id)
            return null;

        return CourseDtoMapper.MapToDetailDto(course);
    }

    public async Task<PaginatedResult<CourseListItemDto>> GetCoursesForTeacherAsync(
        int userId,
        int pageNumber,
        int pageSize,
        int? domainId,
        CourseStatus? status,
        int? subjectId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            throw new InvalidOperationException("Not authorized.");

        var query = _courseRepository.GetTeacherCoursesQueryable(teacher.Id);

        if (domainId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.Subject != null && c.TeacherSubject.Subject.DomainId == domainId.Value);
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);
        if (subjectId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.SubjectId == subjectId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var courses = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = courses.Select(CourseDtoMapper.MapToListItemDto).ToList();
        return new PaginatedResult<CourseListItemDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<CourseDetailDto> CreateCourseAsync(int userId, CreateCourseDto dto, CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            throw new InvalidOperationException("Not authorized.");
        if (teacher.Status != TeacherStatus.Active)
            throw new InvalidOperationException("Teacher account is not active.");

        if (!dto.IsFlexible)
        {
            if (!dto.SessionsCount.HasValue || dto.SessionsCount <= 0)
                throw new InvalidOperationException("SessionsCount is required when course is not flexible.");
            if (!dto.SessionDurationMinutes.HasValue || dto.SessionDurationMinutes <= 0)
                throw new InvalidOperationException("SessionDurationMinutes is required when course is not flexible.");
        }

        var teacherSubject = await _teacherSubjectRepository.GetByIdAsync(dto.TeacherSubjectId);
        if (teacherSubject == null || teacherSubject.TeacherId != teacher.Id || !teacherSubject.IsActive)
            throw new InvalidOperationException("Invalid subject selection. Please select a subject from your active teaching subjects.");

        var teachingMode = await _teachingModeRepository.GetByIdAsync(dto.TeachingModeId);
        if (teachingMode == null)
            throw new InvalidOperationException("Invalid TeachingModeId.");
        var sessionType = await _sessionTypeRepository.GetByIdAsync(dto.SessionTypeId);
        if (sessionType == null)
            throw new InvalidOperationException("Invalid SessionTypeId.");

        var course = new Course
        {
            Title = dto.Title,
            Description = dto.Description,
            IsActive = true,
            TeacherId = teacher.Id,
            TeacherSubjectId = dto.TeacherSubjectId,
            TeachingModeId = dto.TeachingModeId,
            SessionTypeId = dto.SessionTypeId,
            IsFlexible = dto.IsFlexible,
            SessionsCount = dto.SessionsCount,
            SessionDurationMinutes = dto.SessionDurationMinutes,
            Price = dto.Price,
            MaxStudents = dto.MaxStudents,
            CanIncludeInPackages = dto.CanIncludeInPackages,
            Status = CourseStatus.Published,
            CreatedAt = DateTime.UtcNow
        };

        await _courseRepository.AddAsync(course);
        await _courseRepository.SaveChangesAsync();

        var withDetails = await _courseRepository.GetByIdWithDetailsAsync(course.Id);
        return CourseDtoMapper.MapToDetailDto(withDetails ?? course);
    }

    public async Task<CourseDetailDto?> UpdateCourseAsync(int userId, int courseId, UpdateCourseDto dto, CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            throw new InvalidOperationException("Not authorized.");
        if (teacher.Status != TeacherStatus.Active)
            throw new InvalidOperationException("Teacher account is not active.");

        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null || course.TeacherId != teacher.Id)
            return null;

        if (!dto.IsFlexible)
        {
            if (!dto.SessionsCount.HasValue || dto.SessionsCount <= 0)
                throw new InvalidOperationException("SessionsCount is required when course is not flexible.");
            if (!dto.SessionDurationMinutes.HasValue || dto.SessionDurationMinutes <= 0)
                throw new InvalidOperationException("SessionDurationMinutes is required when course is not flexible.");
        }

        var teacherSubject = await _teacherSubjectRepository.GetByIdAsync(dto.TeacherSubjectId);
        if (teacherSubject == null || teacherSubject.TeacherId != teacher.Id || !teacherSubject.IsActive)
            throw new InvalidOperationException("Invalid subject selection. Please select a subject from your active teaching subjects.");

        var teachingMode = await _teachingModeRepository.GetByIdAsync(dto.TeachingModeId);
        if (teachingMode == null)
            throw new InvalidOperationException("Invalid TeachingModeId.");
        var sessionType = await _sessionTypeRepository.GetByIdAsync(dto.SessionTypeId);
        if (sessionType == null)
            throw new InvalidOperationException("Invalid SessionTypeId.");

        course.Title = dto.Title;
        course.Description = dto.Description;
        course.TeacherSubjectId = dto.TeacherSubjectId;
        course.TeachingModeId = dto.TeachingModeId;
        course.SessionTypeId = dto.SessionTypeId;
        course.IsFlexible = dto.IsFlexible;
        course.SessionsCount = dto.SessionsCount;
        course.SessionDurationMinutes = dto.SessionDurationMinutes;
        course.Price = dto.Price;
        course.MaxStudents = dto.MaxStudents;
        course.CanIncludeInPackages = dto.CanIncludeInPackages;
        course.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateAsync(course);
        await _courseRepository.SaveChangesAsync();

        var withDetails = await _courseRepository.GetByIdWithDetailsAsync(course.Id);
        return CourseDtoMapper.MapToDetailDto(withDetails ?? course);
    }

    public async Task<(bool Success, string Message)> DeleteCourseAsync(int userId, int courseId, CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            throw new InvalidOperationException("Not authorized.");
        if (teacher.Status != TeacherStatus.Active)
            throw new InvalidOperationException("Teacher account is not active.");

        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null || course.TeacherId != teacher.Id)
            return (false, "Course not found.");

        var hasEnrollments = await _courseRepository.HasEnrollmentsAsync(course.Id);

        if (hasEnrollments)
        {
            course.IsActive = false;
            course.Status = CourseStatus.Paused;
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.UpdateAsync(course);
            await _courseRepository.SaveChangesAsync();
            return (true, "Course deactivated (has enrollments).");
        }

        await _courseRepository.DeleteAsync(course);
        await _courseRepository.SaveChangesAsync();
        return (true, "Course deleted.");
    }
}
