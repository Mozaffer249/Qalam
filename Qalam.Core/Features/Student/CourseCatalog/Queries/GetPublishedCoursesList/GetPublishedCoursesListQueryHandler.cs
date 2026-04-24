using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;
using StudentEntity = Qalam.Data.Entity.Student.Student;

namespace Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCoursesList;

public class GetPublishedCoursesListQueryHandler : ResponseHandler,
    IRequestHandler<GetPublishedCoursesListQuery, Response<List<CourseCatalogItemDto>>>
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

    public async Task<Response<List<CourseCatalogItemDto>>> Handle(
        GetPublishedCoursesListQuery request,
        CancellationToken cancellationToken)
    {
        StudentEntity? student = null;

        if (request.StudentId.HasValue)
        {
            student = await _studentRepository.GetByIdAsync(request.StudentId.Value);

            if (student == null)
                return NotFound<List<CourseCatalogItemDto>>("Student not found.");

            var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
            if (guardian == null || student.GuardianId != guardian.Id)
                return Forbidden<List<CourseCatalogItemDto>>(
                    "You don't have permission to browse courses for this student.");
        }
        else
        {
            student = await _studentRepository.GetByUserIdAsync(request.UserId);
        }

        // If the client sent any explicit filter, respect only the explicit filters.
        // Otherwise fall back to the student's profile so the default browse view
        // is automatically scoped to what that student studies.
        bool hasExplicitFilter =
            request.DomainId.HasValue ||
            request.CurriculumId.HasValue ||
            request.LevelId.HasValue ||
            request.GradeId.HasValue ||
            request.SubjectId.HasValue ||
            request.TeachingModeId.HasValue;

        int? effectiveDomainId = hasExplicitFilter ? request.DomainId : student?.DomainId;
        int? effectiveCurriculumId = hasExplicitFilter ? request.CurriculumId : student?.CurriculumId;
        int? effectiveLevelId = hasExplicitFilter ? request.LevelId : student?.LevelId;
        int? effectiveGradeId = hasExplicitFilter ? request.GradeId : student?.GradeId;

        var query = _courseRepository.GetPublishedCoursesQueryable();

        if (effectiveDomainId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.Subject != null && c.TeacherSubject.Subject.DomainId == effectiveDomainId.Value);
        if (effectiveCurriculumId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.Subject != null && c.TeacherSubject.Subject.CurriculumId == effectiveCurriculumId.Value);
        if (effectiveLevelId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.Subject != null && c.TeacherSubject.Subject.LevelId == effectiveLevelId.Value);
        if (effectiveGradeId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.Subject != null && c.TeacherSubject.Subject.GradeId == effectiveGradeId.Value);
        if (request.SubjectId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.SubjectId == request.SubjectId.Value);
        if (request.TeachingModeId.HasValue)
            query = query.Where(c => c.TeachingModeId == request.TeachingModeId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var courses = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = _mapper.Map<List<CourseCatalogItemDto>>(courses);

        var totalPages = request.PageSize > 0
            ? (int)Math.Ceiling(totalCount / (double)request.PageSize)
            : 0;

        var meta = new
        {
            totalCount,
            pageNumber = request.PageNumber,
            pageSize = request.PageSize,
            totalPages,
            hasPreviousPage = request.PageNumber > 1,
            hasNextPage = request.PageNumber < totalPages
        };

        return Success(entity: items, Meta: meta);
    }
}
