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
    private readonly ICourseSessionUnitRepository _courseSessionUnitRepository;
    private readonly ITeacherSubjectRepertoireService _repertoireService;

    public TeacherCourseService(
        ITeacherRepository teacherRepository,
        ICourseRepository courseRepository,
        ITeacherSubjectRepository teacherSubjectRepository,
        ITeachingModeRepository teachingModeRepository,
        ISessionTypeRepository sessionTypeRepository,
        ICourseSessionUnitRepository courseSessionUnitRepository,
        ITeacherSubjectRepertoireService repertoireService)
    {
        _teacherRepository = teacherRepository;
        _courseRepository = courseRepository;
        _teacherSubjectRepository = teacherSubjectRepository;
        _teachingModeRepository = teachingModeRepository;
        _sessionTypeRepository = sessionTypeRepository;
        _courseSessionUnitRepository = courseSessionUnitRepository;
        _repertoireService = repertoireService;
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

        if (dto.IsFlexible)
            throw new InvalidOperationException("Flexible courses are not supported. Create a fixed course with a session plan.");

        if (dto.SessionDurationMinutes.HasValue && dto.SessionDurationMinutes <= 0)
            throw new InvalidOperationException("SessionDurationMinutes must be greater than zero when provided.");
        if (dto.Sessions == null || dto.Sessions.Count == 0)
            throw new InvalidOperationException("Sessions are required for fixed courses.");

        var teacherSubject = await _teacherSubjectRepository.GetByIdForTeacherAsync(
            teacher.Id, dto.TeacherSubjectId, cancellationToken);
        if (teacherSubject == null
            || !teacherSubject.IsActive
            || teacherSubject.VerificationStatus != DocumentVerificationStatus.Approved)
            throw new InvalidOperationException("Invalid subject selection. Please select a subject from your active teaching subjects.");

        var teachingMode = await _teachingModeRepository.GetByIdAsync(dto.TeachingModeId);
        if (teachingMode == null)
            throw new InvalidOperationException("Invalid TeachingModeId.");
        var sessionType = await _sessionTypeRepository.GetByIdAsync(dto.SessionTypeId);
        if (sessionType == null)
            throw new InvalidOperationException("Invalid SessionTypeId.");
        var isGroupSession = string.Equals(sessionType.Code, "group", StringComparison.OrdinalIgnoreCase);
        if (isGroupSession)
        {
            if (!dto.MaxStudents.HasValue || dto.MaxStudents.Value < 2)
                throw new InvalidOperationException("MaxStudents is required and must be >= 2 for group courses.");
        }
        else if (dto.MaxStudents.HasValue)
        {
            throw new InvalidOperationException("MaxStudents must be null for individual courses.");
        }

        // Subject-consistency check: every ContentUnit / Lesson attached to any session must
        // belong to the course's subject. Batched into two queries so 50 sessions × N units don't
        // turn into N+1 lookups.
        await ValidateSessionUnitsBelongToSubjectAsync(dto.Sessions, teacherSubject.SubjectId, cancellationToken);

        var repertoireError = await _repertoireService.ValidateSessionUnitsInRepertoireAsync(
            teacherSubject, dto.Sessions, cancellationToken);
        if (repertoireError != null)
            throw new InvalidOperationException(repertoireError);

        var course = new Course
        {
            Title = dto.Title,
            Description = dto.Description,
            IsActive = true,
            TeacherId = teacher.Id,
            TeacherSubjectId = dto.TeacherSubjectId,
            TeachingModeId = dto.TeachingModeId,
            SessionTypeId = dto.SessionTypeId,
            IsFlexible = false,
            SessionDurationMinutes = dto.SessionDurationMinutes,
            Price = dto.Price,
            MaxStudents = dto.MaxStudents,
            CanIncludeInPackages = dto.CanIncludeInPackages,
            ImageUrl = dto.ImageUrl,
            Status = CourseStatus.Published,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.Sessions != null && dto.Sessions.Count > 0)
        {
            course.Sessions = dto.Sessions
                .Select((s, i) =>
                {
                    var session = new CourseSession
                    {
                        SessionNumber = i + 1,
                        DurationMinutes = s.DurationMinutes,
                        Title = s.Title,
                        Notes = s.Notes,
                        CreatedAt = DateTime.UtcNow
                    };
                    if (s.Units != null)
                    {
                        foreach (var u in s.Units)
                        {
                            session.Units.Add(new CourseSessionUnit
                            {
                                ContentUnitId = u.ContentUnitId,
                                LessonId = u.LessonId,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    return session;
                })
                .ToList();
        }

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
            if (dto.SessionDurationMinutes.HasValue && dto.SessionDurationMinutes <= 0)
                throw new InvalidOperationException("SessionDurationMinutes must be greater than zero when provided.");
        }
        else
        {
            throw new InvalidOperationException("Flexible courses are not supported. Keep the course fixed with a session plan.");
        }

        var teacherSubject = await _teacherSubjectRepository.GetByIdAsync(dto.TeacherSubjectId);
        if (teacherSubject == null
            || teacherSubject.TeacherId != teacher.Id
            || !teacherSubject.IsActive
            || teacherSubject.VerificationStatus != DocumentVerificationStatus.Approved)
            throw new InvalidOperationException("Invalid subject selection. Please select a subject from your active teaching subjects.");

        var teachingMode = await _teachingModeRepository.GetByIdAsync(dto.TeachingModeId);
        if (teachingMode == null)
            throw new InvalidOperationException("Invalid TeachingModeId.");
        var sessionType = await _sessionTypeRepository.GetByIdAsync(dto.SessionTypeId);
        if (sessionType == null)
            throw new InvalidOperationException("Invalid SessionTypeId.");
        var isGroupSessionForUpdate = string.Equals(sessionType.Code, "group", StringComparison.OrdinalIgnoreCase);
        if (isGroupSessionForUpdate)
        {
            if (!dto.MaxStudents.HasValue || dto.MaxStudents.Value < 2)
                throw new InvalidOperationException("MaxStudents is required and must be >= 2 for group courses.");
        }
        else if (dto.MaxStudents.HasValue)
        {
            throw new InvalidOperationException("MaxStudents must be null for individual courses.");
        }

        course.Title = dto.Title;
        course.Description = dto.Description;
        course.TeacherSubjectId = dto.TeacherSubjectId;
        course.TeachingModeId = dto.TeachingModeId;
        course.SessionTypeId = dto.SessionTypeId;
        course.IsFlexible = false;
        course.SessionDurationMinutes = dto.SessionDurationMinutes;
        course.Price = dto.Price;
        course.MaxStudents = dto.MaxStudents;
        course.CanIncludeInPackages = dto.CanIncludeInPackages;
        if (dto.ImageUrl != null)
            course.ImageUrl = dto.ImageUrl;
        course.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateAsync(course);
        await _courseRepository.SaveChangesAsync();

        var withDetails = await _courseRepository.GetByIdWithDetailsAsync(course.Id);
        return CourseDtoMapper.MapToDetailDto(withDetails ?? course);
    }

    public async Task<List<CourseSessionUnitDto>?> ReplaceSessionUnitsAsync(
        int userId,
        int courseId,
        int sessionId,
        List<CreateCourseSessionUnitDto> units,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            throw new InvalidOperationException("Not authorized.");
        if (teacher.Status != TeacherStatus.Active)
            throw new InvalidOperationException("Teacher account is not active.");

        var subjectId = await _courseSessionUnitRepository.GetSubjectIdForOwnedSessionAsync(sessionId, courseId, teacher.Id, cancellationToken);
        if (subjectId == null)
            return null;

        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null || course.TeacherId != teacher.Id)
            return null;

        var teacherSubject = await _teacherSubjectRepository.GetByIdForTeacherAsync(
            teacher.Id, course.TeacherSubjectId, cancellationToken);
        if (teacherSubject == null
            || !teacherSubject.IsActive
            || teacherSubject.VerificationStatus != DocumentVerificationStatus.Approved)
            throw new InvalidOperationException("Invalid subject selection. Please select a subject from your active teaching subjects.");

        // Subject-consistency check is delegated to the repo (single batched read per FK kind).
        var contentUnitIds = units.Where(u => u.ContentUnitId.HasValue).Select(u => u.ContentUnitId!.Value).Distinct().ToList();
        var lessonIds = units.Where(u => u.LessonId.HasValue).Select(u => u.LessonId!.Value).Distinct().ToList();
        await _courseSessionUnitRepository.ValidateUnitsBelongToSubjectAsync(contentUnitIds, lessonIds, subjectId.Value, cancellationToken);

        var repertoireError = await _repertoireService.ValidateUnitRowsInRepertoireAsync(
            teacherSubject, units, cancellationToken);
        if (repertoireError != null)
            throw new InvalidOperationException(repertoireError);

        var now = DateTime.UtcNow;
        var newRows = units.Select(u => new CourseSessionUnit
        {
            CourseSessionId = sessionId,
            ContentUnitId = u.ContentUnitId,
            LessonId = u.LessonId,
            CreatedAt = now
        });

        await _courseSessionUnitRepository.ReplaceUnitsAsync(sessionId, newRows, cancellationToken);

        return await _courseSessionUnitRepository.GetHydratedDtosBySessionAsync(sessionId, cancellationToken);
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

    /// <summary>
    /// Confirms every selected ContentUnit / Lesson across all sessions belongs to the course's
    /// subject. Two batched queries (one per FK) keep this O(1) round-trips regardless of session
    /// or unit count. Throws InvalidOperationException on mismatch — the caller's handler turns
    /// that into BadRequest.
    /// </summary>
    private async Task ValidateSessionUnitsBelongToSubjectAsync(
        List<CreateCourseSessionDto>? sessions,
        int subjectId,
        CancellationToken cancellationToken)
    {
        if (sessions == null) return;

        var contentUnitIds = sessions
            .Where(s => s.Units != null)
            .SelectMany(s => s.Units!)
            .Where(u => u.ContentUnitId.HasValue)
            .Select(u => u.ContentUnitId!.Value)
            .Distinct()
            .ToList();

        var lessonIds = sessions
            .Where(s => s.Units != null)
            .SelectMany(s => s.Units!)
            .Where(u => u.LessonId.HasValue)
            .Select(u => u.LessonId!.Value)
            .Distinct()
            .ToList();

        await _courseSessionUnitRepository.ValidateUnitsBelongToSubjectAsync(contentUnitIds, lessonIds, subjectId, cancellationToken);
    }
}
