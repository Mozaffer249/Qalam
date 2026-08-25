using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Mappers;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;

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
    private readonly IMediaUrlResolver _mediaUrlResolver;
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingMarketResolver _marketResolver;
    private readonly ITeacherDomainPricingRepository _domainPricingRepository;

    public TeacherCourseService(
        ITeacherRepository teacherRepository,
        ICourseRepository courseRepository,
        ITeacherSubjectRepository teacherSubjectRepository,
        ITeachingModeRepository teachingModeRepository,
        ISessionTypeRepository sessionTypeRepository,
        ICourseSessionUnitRepository courseSessionUnitRepository,
        ITeacherSubjectRepertoireService repertoireService,
        IMediaUrlResolver mediaUrlResolver,
        IPricingEngine pricingEngine,
        IPricingMarketResolver marketResolver,
        ITeacherDomainPricingRepository domainPricingRepository)
    {
        _teacherRepository = teacherRepository;
        _courseRepository = courseRepository;
        _teacherSubjectRepository = teacherSubjectRepository;
        _teachingModeRepository = teachingModeRepository;
        _sessionTypeRepository = sessionTypeRepository;
        _courseSessionUnitRepository = courseSessionUnitRepository;
        _repertoireService = repertoireService;
        _mediaUrlResolver = mediaUrlResolver;
        _pricingEngine = pricingEngine;
        _marketResolver = marketResolver;
        _domainPricingRepository = domainPricingRepository;
    }

    public async Task<CourseDetailDto?> GetCourseByIdForTeacherAsync(int userId, int courseId, CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            return null;

        var course = await _courseRepository.GetByIdWithDetailsAsync(courseId);
        if (course == null || course.TeacherId != teacher.Id)
            return null;

        return await EnrichWithTeacherMarketAsync(userId, WithPublicImageUrl(CourseDtoMapper.MapToDetailDto(course)), course, cancellationToken);
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

        var items = new List<CourseListItemDto>();
        var market = await _marketResolver.ResolveForUserAsync(userId, cancellationToken);
        foreach (var course in courses)
        {
            var item = CourseDtoMapper.MapToListItemDto(course);
            item.Currency = market.Currency;
            item.MarketCode = market.MarketCode;
            item.Price = await ResolveStudentHourlyAsync(
                course.DomainId,
                course.SessionType?.Code ?? "individual",
                market.MarketCode,
                teacher.Id,
                course.Price,
                cancellationToken);
            items.Add(item);
        }
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
        if (teacherSubject == null || !teacherSubject.IsActive)
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

        var quranRequiredError = ValidateSessionsQuranRequired(teacherSubject, dto.Sessions);
        if (quranRequiredError != null)
            throw new InvalidOperationException(quranRequiredError);

        var quranError = ValidateSessionsQuranCoverage(teacherSubject, dto.Sessions);
        if (quranError != null)
            throw new InvalidOperationException(quranError);

        var domainId = teacherSubject.Subject?.DomainId
            ?? throw new InvalidOperationException("Subject domain is required for pricing.");
        var market = await _marketResolver.ResolveForUserAsync(userId, cancellationToken);
        var estimate = await _pricingEngine.EstimateAsync(new PricingEstimateRequest
        {
            DomainId = domainId,
            SessionTypeCode = sessionType.Code,
            MarketCode = market.MarketCode,
            TotalMinutes = 60,
            TeacherId = teacher.Id
        }, cancellationToken);
        var pricePerHour = estimate.PricePerHour;

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
            Price = pricePerHour,
            MaxStudents = dto.MaxStudents,
            CanIncludeInPackages = dto.CanIncludeInPackages,
            ImageUrl = dto.ImageUrl,
            Status = dto.Publish ? CourseStatus.Published : CourseStatus.Draft,
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
                        QuranContentTypeId = s.QuranContentTypeId,
                        QuranLevelId = s.QuranLevelId,
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
                                CustomUnitLabel = string.IsNullOrWhiteSpace(u.CustomUnitLabel) ? null : u.CustomUnitLabel.Trim(),
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
        return await EnrichWithTeacherMarketAsync(
            userId,
            WithPublicImageUrl(CourseDtoMapper.MapToDetailDto(withDetails ?? course)),
            withDetails ?? course,
            cancellationToken);
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

        if (course.Status == CourseStatus.Paused)
            throw new InvalidOperationException("COURSE_EDIT_LOCKED_PAUSED");

        if (await _courseRepository.HasEnrollmentsAsync(course.Id))
            throw new InvalidOperationException("COURSE_EDIT_LOCKED_ENROLLMENTS");

        if (!dto.IsFlexible)
        {
            if (dto.SessionDurationMinutes.HasValue && dto.SessionDurationMinutes <= 0)
                throw new InvalidOperationException("SessionDurationMinutes must be greater than zero when provided.");
        }
        else
        {
            throw new InvalidOperationException("Flexible courses are not supported. Keep the course fixed with a session plan.");
        }

        var teacherSubject = await _teacherSubjectRepository.GetByIdForTeacherAsync(
            teacher.Id, dto.TeacherSubjectId, cancellationToken);
        if (teacherSubject == null
            || !teacherSubject.IsActive)
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

        var domainId = teacherSubject.Subject?.DomainId
            ?? throw new InvalidOperationException("Subject domain is required for pricing.");
        var market = await _marketResolver.ResolveForUserAsync(userId, cancellationToken);
        var estimate = await _pricingEngine.EstimateAsync(new PricingEstimateRequest
        {
            DomainId = domainId,
            SessionTypeCode = sessionType.Code,
            MarketCode = market.MarketCode,
            TotalMinutes = 60,
            TeacherId = teacher.Id
        }, cancellationToken);
        var pricePerHour = estimate.PricePerHour;

        course.Title = dto.Title;
        course.Description = dto.Description;
        course.TeacherSubjectId = dto.TeacherSubjectId;
        course.TeachingModeId = dto.TeachingModeId;
        course.SessionTypeId = dto.SessionTypeId;
        course.IsFlexible = false;
        course.SessionDurationMinutes = dto.SessionDurationMinutes;
        course.Price = pricePerHour;
        course.MaxStudents = dto.MaxStudents;
        course.CanIncludeInPackages = dto.CanIncludeInPackages;
        if (dto.ImageUrl != null)
            course.ImageUrl = dto.ImageUrl;
        course.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateAsync(course);
        await _courseRepository.SaveChangesAsync();

        var withDetails = await _courseRepository.GetByIdWithDetailsAsync(course.Id);
        return await EnrichWithTeacherMarketAsync(
            userId,
            WithPublicImageUrl(CourseDtoMapper.MapToDetailDto(withDetails ?? course)),
            withDetails ?? course,
            cancellationToken);
    }

    public async Task<CourseDetailDto?> PublishCourseAsync(
        int userId,
        int courseId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await RequireActiveTeacherAsync(userId);
        var course = await GetOwnedCourseAsync(teacher.Id, courseId);
        if (course == null)
            return null;

        if (course.Status != CourseStatus.Draft)
            throw new InvalidOperationException("COURSE_INVALID_STATUS_TRANSITION");

        var withDetails = await _courseRepository.GetByIdWithDetailsAsync(course.Id)
            ?? course;
        var domainId = withDetails.DomainId;
        if (domainId <= 0)
            throw new InvalidOperationException("Subject domain is required for pricing.");

        var sessionTypeCode = withDetails.SessionType?.Code ?? "individual";
        var market = await _marketResolver.ResolveForUserAsync(userId, cancellationToken);
        var estimate = await _pricingEngine.EstimateAsync(new PricingEstimateRequest
        {
            DomainId = domainId,
            SessionTypeCode = sessionTypeCode,
            MarketCode = market.MarketCode,
            TotalMinutes = 60,
            TeacherId = teacher.Id
        }, cancellationToken);

        course.Price = estimate.PricePerHour;
        course.Status = CourseStatus.Published;
        course.IsActive = true;
        course.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateAsync(course);
        await _courseRepository.SaveChangesAsync();

        withDetails = await _courseRepository.GetByIdWithDetailsAsync(course.Id);
        return await EnrichWithTeacherMarketAsync(
            userId,
            WithPublicImageUrl(CourseDtoMapper.MapToDetailDto(withDetails ?? course)),
            withDetails ?? course,
            cancellationToken);
    }

    public async Task<CourseDetailDto?> PauseCourseAsync(
        int userId,
        int courseId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await RequireActiveTeacherAsync(userId);
        var course = await GetOwnedCourseAsync(teacher.Id, courseId);
        if (course == null)
            return null;

        if (course.Status != CourseStatus.Published)
            throw new InvalidOperationException("COURSE_INVALID_STATUS_TRANSITION");

        course.Status = CourseStatus.Paused;
        course.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateAsync(course);
        await _courseRepository.SaveChangesAsync();

        var withDetails = await _courseRepository.GetByIdWithDetailsAsync(course.Id);
        return await EnrichWithTeacherMarketAsync(
            userId,
            WithPublicImageUrl(CourseDtoMapper.MapToDetailDto(withDetails ?? course)),
            withDetails ?? course,
            cancellationToken);
    }

    public async Task<CourseDetailDto?> ReactivateCourseAsync(
        int userId,
        int courseId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await RequireActiveTeacherAsync(userId);
        var course = await GetOwnedCourseAsync(teacher.Id, courseId);
        if (course == null)
            return null;

        if (course.Status != CourseStatus.Paused)
            throw new InvalidOperationException("COURSE_INVALID_STATUS_TRANSITION");

        course.Status = CourseStatus.Published;
        course.IsActive = true;
        course.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateAsync(course);
        await _courseRepository.SaveChangesAsync();

        var withDetails = await _courseRepository.GetByIdWithDetailsAsync(course.Id);
        return await EnrichWithTeacherMarketAsync(
            userId,
            WithPublicImageUrl(CourseDtoMapper.MapToDetailDto(withDetails ?? course)),
            withDetails ?? course,
            cancellationToken);
    }

    private async Task<Data.Entity.Teacher.Teacher> RequireActiveTeacherAsync(int userId)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            throw new InvalidOperationException("Not authorized.");
        if (teacher.Status != TeacherStatus.Active)
            throw new InvalidOperationException("Teacher account is not active.");
        return teacher;
    }

    private async Task<Course?> GetOwnedCourseAsync(int teacherId, int courseId)
    {
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null || course.TeacherId != teacherId)
            return null;
        return course;
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
        if (teacherSubject == null || !teacherSubject.IsActive)
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
            CustomUnitLabel = string.IsNullOrWhiteSpace(u.CustomUnitLabel) ? null : u.CustomUnitLabel.Trim(),
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

    /// <summary>
    /// Quran domain: every session must include type + level.
    /// Non-Quran: both must be null.
    /// </summary>
    private static string? ValidateSessionsQuranRequired(
        Data.Entity.Teacher.TeacherSubject teacherSubject,
        List<CreateCourseSessionDto>? sessions)
    {
        if (sessions == null || sessions.Count == 0) return null;

        var domain = teacherSubject.Subject?.Domain;
        var isQuran = QuranDomainHelper.IsQuranDomain(domain?.Code, domain?.NameEn);

        for (var i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            var hasType = session.QuranContentTypeId.HasValue;
            var hasLevel = session.QuranLevelId.HasValue;

            if (isQuran)
            {
                if (!hasType || !hasLevel)
                    return "جلسات مجال القرآن تتطلب QuranContentTypeId و QuranLevelId";
            }
            else if (hasType || hasLevel)
            {
                return $"Session {i + 1}: QuranContentTypeId and QuranLevelId are only allowed for Quran domain subjects.";
            }
        }

        return null;
    }

    /// <summary>
    /// Empty teacher Quran coverage sets mean all types/levels are allowed.
    /// </summary>
    private static string? ValidateSessionsQuranCoverage(
        Data.Entity.Teacher.TeacherSubject teacherSubject,
        List<CreateCourseSessionDto>? sessions)
    {
        if (sessions == null) return null;

        var coveredTypes = teacherSubject.QuranContentTypes
            .Select(c => c.QuranContentTypeId)
            .ToHashSet();
        var coveredLevels = teacherSubject.QuranLevels
            .Select(l => l.QuranLevelId)
            .ToHashSet();

        for (var i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            var label = $"Session {i + 1}";

            if (session.QuranContentTypeId is int typeId
                && coveredTypes.Count > 0
                && !coveredTypes.Contains(typeId))
            {
                return $"{label}: Quran content type {typeId} is outside this teacher's coverage.";
            }

            if (session.QuranLevelId is int levelId
                && coveredLevels.Count > 0
                && !coveredLevels.Contains(levelId))
            {
                return $"{label}: Quran level {levelId} is outside this teacher's coverage.";
            }
        }

        return null;
    }

    private CourseDetailDto WithPublicImageUrl(CourseDetailDto dto)
    {
        dto.ImageUrl = _mediaUrlResolver.ToPublicUrl(dto.ImageUrl);
        return dto;
    }

    private async Task<CourseDetailDto> EnrichWithTeacherMarketAsync(
        int userId,
        CourseDetailDto dto,
        Course course,
        CancellationToken cancellationToken)
    {
        var market = await _marketResolver.ResolveForUserAsync(userId, cancellationToken);
        dto.Currency = market.Currency;
        dto.MarketCode = market.MarketCode;
        dto.Price = await ResolveStudentHourlyAsync(
            course.DomainId,
            course.SessionType?.Code ?? "individual",
            market.MarketCode,
            course.TeacherId,
            course.Price,
            cancellationToken);

        var hasBlocking = await _courseRepository.HasEnrollmentsAsync(course.Id);
        dto.HasBlockingEnrollments = hasBlocking;
        dto.CanEdit = course.Status != CourseStatus.Paused && !hasBlocking;

        if (course.DomainId > 0 && course.TeacherId > 0)
        {
            var domainPricing = await _domainPricingRepository.GetByTeacherAndDomainAsync(
                course.TeacherId,
                course.DomainId,
                cancellationToken);
            dto.InterviewPending = domainPricing == null || !domainPricing.HasCompletedInterviewSession;
        }

        return dto;
    }

    /// <summary>
    /// Student-facing hourly rate (platform catalog or reflected custom). Falls back to
    /// <paramref name="fallbackPrice"/> when no rate is configured.
    /// </summary>
    private async Task<decimal> ResolveStudentHourlyAsync(
        int domainId,
        string sessionTypeCode,
        string marketCode,
        int teacherId,
        decimal fallbackPrice,
        CancellationToken cancellationToken)
    {
        if (domainId <= 0 || teacherId <= 0)
            return fallbackPrice;

        try
        {
            var estimate = await _pricingEngine.EstimateAsync(new PricingEstimateRequest
            {
                DomainId = domainId,
                SessionTypeCode = sessionTypeCode,
                MarketCode = marketCode,
                TotalMinutes = 60,
                TeacherId = teacherId
            }, cancellationToken);
            return estimate.PricePerHour;
        }
        catch (InvalidOperationException)
        {
            return fallbackPrice;
        }
    }
}
