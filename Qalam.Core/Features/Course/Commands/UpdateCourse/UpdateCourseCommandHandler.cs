using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using CourseEntity = Qalam.Data.Entity.Course.Course;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Course.Commands.UpdateCourse;

public class UpdateCourseCommandHandler : ResponseHandler,
    IRequestHandler<UpdateCourseCommand, Response<CourseDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IEducationDomainRepository _domainRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ITeachingModeRepository _teachingModeRepository;
    private readonly ISessionTypeRepository _sessionTypeRepository;

    public UpdateCourseCommandHandler(
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
        UpdateCourseCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<CourseDetailDto>("Teacher not found.");

        var course = await _courseRepository.GetByIdAsync(request.Id) as CourseEntity;
        if (course == null)
            return NotFound<CourseDetailDto>("Course not found.");
        if (course.TeacherId != teacher.Id)
            return NotFound<CourseDetailDto>("Course not found.");

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

        course.Title = dto.Title;
        course.Description = dto.Description;
        course.DomainId = dto.DomainId;
        course.SubjectId = dto.SubjectId;
        course.CurriculumId = dto.CurriculumId;
        course.LevelId = dto.LevelId;
        course.GradeId = dto.GradeId;
        course.TeachingModeId = dto.TeachingModeId;
        course.SessionTypeId = dto.SessionTypeId;
        course.IsFlexible = dto.IsFlexible;
        course.SessionsCount = dto.SessionsCount;
        course.SessionDurationMinutes = dto.SessionDurationMinutes;
        course.Price = dto.Price;
        course.MaxStudents = dto.MaxStudents;
        course.CanIncludeInPackages = dto.CanIncludeInPackages;
        course.StartDate = dto.StartDate;
        course.EndDate = dto.EndDate;
        course.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateAsync(course);
        await _courseRepository.SaveChangesAsync();

        var withDetails = await _courseRepository.GetByIdWithDetailsAsync(course.Id);
        var detail = withDetails != null
            ? CourseDtoMapper.MapToDetailDto(withDetails)
            : CourseDtoMapper.MapToDetailDto(course, domain.NameEn, subject.NameEn, teachingMode.NameEn, sessionType.NameEn, teacher.Id, null);

        return Success(entity: detail);
    }
}
