using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using CourseEntity = Qalam.Data.Entity.Course.Course;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Course.Commands.CreateCourse;

public class CreateCourseCommandHandler : ResponseHandler,
    IRequestHandler<CreateCourseCommand, Response<CourseDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IEducationDomainRepository _domainRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ITeachingModeRepository _teachingModeRepository;
    private readonly ISessionTypeRepository _sessionTypeRepository;

    public CreateCourseCommandHandler(
        ITeacherRepository teacherRepository,
        ICourseRepository courseRepository,
        IEducationDomainRepository domainRepository,
        ISubjectRepository subjectRepository,
        ITeachingModeRepository teachingModeRepository,
        ISessionTypeRepository sessionTypeRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _courseRepository = courseRepository;
        _domainRepository = domainRepository;
        _subjectRepository = subjectRepository;
        _teachingModeRepository = teachingModeRepository;
        _sessionTypeRepository = sessionTypeRepository;
    }

    public async Task<Response<CourseDetailDto>> Handle(
        CreateCourseCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<CourseDetailDto>("Teacher not found.");

        var dto = request.Data;

        if (!dto.IsFlexible)
        {
            if (!dto.SessionsCount.HasValue || dto.SessionsCount <= 0)
                return BadRequest<CourseDetailDto>("SessionsCount is required when course is not flexible.");
            if (!dto.SessionDurationMinutes.HasValue || dto.SessionDurationMinutes <= 0)
                return BadRequest<CourseDetailDto>("SessionDurationMinutes is required when course is not flexible.");
        }

        if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
            return BadRequest<CourseDetailDto>("EndDate must be on or after StartDate.");

        var domain = await _domainRepository.GetByIdAsync(dto.DomainId);
        if (domain == null)
            return BadRequest<CourseDetailDto>("Invalid DomainId.");
        var subject = await _subjectRepository.GetByIdAsync(dto.SubjectId);
        if (subject == null)
            return BadRequest<CourseDetailDto>("Invalid SubjectId.");
        var teachingMode = await _teachingModeRepository.GetByIdAsync(dto.TeachingModeId);
        if (teachingMode == null)
            return BadRequest<CourseDetailDto>("Invalid TeachingModeId.");
        var sessionType = await _sessionTypeRepository.GetByIdAsync(dto.SessionTypeId);
        if (sessionType == null)
            return BadRequest<CourseDetailDto>("Invalid SessionTypeId.");

        var course = new CourseEntity
        {
            Title = dto.Title,
            Description = dto.Description,
            IsActive = true,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TeacherId = teacher.Id,
            DomainId = dto.DomainId,
            SubjectId = dto.SubjectId,
            CurriculumId = dto.CurriculumId,
            LevelId = dto.LevelId,
            GradeId = dto.GradeId,
            TeachingModeId = dto.TeachingModeId,
            SessionTypeId = dto.SessionTypeId,
            IsFlexible = dto.IsFlexible,
            SessionsCount = dto.SessionsCount,
            SessionDurationMinutes = dto.SessionDurationMinutes,
            Price = dto.Price,
            MaxStudents = dto.MaxStudents,
            CanIncludeInPackages = dto.CanIncludeInPackages,
            Status = CourseStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        await _courseRepository.AddAsync(course);
        await _courseRepository.SaveChangesAsync();

        var withDetails = await _courseRepository.GetByIdWithDetailsAsync(course.Id);
        var detail = withDetails != null
            ? CourseDtoMapper.MapToDetailDto(withDetails)
            : CourseDtoMapper.MapToDetailDto(course, domain.NameEn, subject.NameEn, teachingMode.NameEn, sessionType.NameEn, teacher.Id, null);
        return Success(entity: detail);
    }
}
