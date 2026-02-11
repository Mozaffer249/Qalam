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

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.CreateCourse;

public class CreateCourseCommandHandler : ResponseHandler,
    IRequestHandler<CreateCourseCommand, Response<CourseDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly ITeachingModeRepository _teachingModeRepository;
    private readonly ISessionTypeRepository _sessionTypeRepository;

    public CreateCourseCommandHandler(
        ITeacherRepository teacherRepository,
        ICourseRepository courseRepository,
        ITeacherSubjectRepository teacherSubjectRepository,
        ITeachingModeRepository teachingModeRepository,
        ISessionTypeRepository sessionTypeRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _courseRepository = courseRepository;
        _teacherSubjectRepository = teacherSubjectRepository;
        _teachingModeRepository = teachingModeRepository;
        _sessionTypeRepository = sessionTypeRepository;
    }

    public async Task<Response<CourseDetailDto>> Handle(
        CreateCourseCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return Unauthorized<CourseDetailDto>("Not authorized.");
        
        if (teacher.Status != TeacherStatus.Active)
            return BadRequest<CourseDetailDto>("Teacher account is not active.");

        var dto = request.Data;

        if (!dto.IsFlexible)
        {
            if (!dto.SessionsCount.HasValue || dto.SessionsCount <= 0)
                return BadRequest<CourseDetailDto>("SessionsCount is required when course is not flexible.");
            if (!dto.SessionDurationMinutes.HasValue || dto.SessionDurationMinutes <= 0)
                return BadRequest<CourseDetailDto>("SessionDurationMinutes is required when course is not flexible.");
        }

        var teacherSubject = await _teacherSubjectRepository.GetByIdAsync(dto.TeacherSubjectId);
        if (teacherSubject == null || teacherSubject.TeacherId != teacher.Id || !teacherSubject.IsActive)
            return BadRequest<CourseDetailDto>("Invalid subject selection. Please select a subject from your active teaching subjects.");
        
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
            Status = CourseStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        await _courseRepository.AddAsync(course);
        await _courseRepository.SaveChangesAsync();

        var withDetails = await _courseRepository.GetByIdWithDetailsAsync(course.Id);
        var detail = CourseDtoMapper.MapToDetailDto(withDetails ?? course);
        return Success(entity: detail);
    }
}
