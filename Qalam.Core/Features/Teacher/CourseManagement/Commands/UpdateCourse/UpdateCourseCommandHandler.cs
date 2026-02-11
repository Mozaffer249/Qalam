using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using CourseEntity = Qalam.Data.Entity.Course.Course;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.UpdateCourse;

public class UpdateCourseCommandHandler : ResponseHandler,
    IRequestHandler<UpdateCourseCommand, Response<CourseDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly ITeachingModeRepository _teachingModeRepository;
    private readonly ISessionTypeRepository _sessionTypeRepository;

    public UpdateCourseCommandHandler(
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
        UpdateCourseCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return Unauthorized<CourseDetailDto>("Not authorized.");
        
        if (teacher.Status != TeacherStatus.Active)
            return BadRequest<CourseDetailDto>("Teacher account is not active.");

        var course = await _courseRepository.GetByIdAsync(request.Id) as CourseEntity;
        if (course == null)
            return NotFound<CourseDetailDto>("Course not found.");
        if (course.TeacherId != teacher.Id)
            return Forbidden<CourseDetailDto>("Access denied.");

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
        var detail = CourseDtoMapper.MapToDetailDto(withDetails ?? course);

        return Success(entity: detail);
    }
}
