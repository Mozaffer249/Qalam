using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using StudentEntity = Qalam.Data.Entity.Student.Student;

namespace Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCoursesList;

public class GetPublishedCoursesListQueryHandler : ResponseHandler,
    IRequestHandler<GetPublishedCoursesListQuery, Response<PaginatedResult<CourseCatalogItemDto>>>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly IMapper _mapper;

    public GetPublishedCoursesListQueryHandler(
        ICourseRepository courseRepository,
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _courseRepository = courseRepository;
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _mapper = mapper;
    }

    public async Task<Response<PaginatedResult<CourseCatalogItemDto>>> Handle(
        GetPublishedCoursesListQuery request,
        CancellationToken cancellationToken)
    {
        StudentEntity? student = null;

        if (request.StudentId.HasValue)
        {
            // Guardian browsing for a specific child
            student = await _studentRepository.GetByIdAsync(request.StudentId.Value);

            if (student == null)
                return NotFound<PaginatedResult<CourseCatalogItemDto>>("Student not found.");

            // Security: Verify the student is a child of this guardian
            var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
            if (guardian == null || student.GuardianId != guardian.Id)
                return Forbidden<PaginatedResult<CourseCatalogItemDto>>(
                    "You don't have permission to browse courses for this student.");
        }
        else
        {
            // Default behavior: Student browsing for themselves
            student = await _studentRepository.GetByUserIdAsync(request.UserId);
        }

        // Use student profile as default filters
        var effectiveDomainId = request.DomainId ?? student?.DomainId;
        var effectiveCurriculumId = request.CurriculumId ?? student?.CurriculumId;
        var effectiveLevelId = request.LevelId ?? student?.LevelId;
        var effectiveGradeId = request.GradeId ?? student?.GradeId;

        var query = _courseRepository.GetPublishedCoursesQueryable();

        // Apply filters
        if (effectiveDomainId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.Subject != null && c.TeacherSubject.Subject.DomainId == effectiveDomainId.Value);
        if (effectiveCurriculumId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.CurriculumId == effectiveCurriculumId.Value);
        if (effectiveLevelId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.LevelId == effectiveLevelId.Value);
        if (effectiveGradeId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.GradeId == effectiveGradeId.Value);
        if (request.SubjectId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.SubjectId == request.SubjectId.Value);
        if (request.TeachingModeId.HasValue)
            query = query.Where(c => c.TeachingModeId == request.TeachingModeId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        // Get paginated courses and use AutoMapper
        var courses = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = _mapper.Map<List<CourseCatalogItemDto>>(courses);

        var result = new PaginatedResult<CourseCatalogItemDto>(items, totalCount, request.PageNumber, request.PageSize);
        return Success(entity: result);
    }
}
