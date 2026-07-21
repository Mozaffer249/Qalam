using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using System.Globalization;

namespace Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCoursesList;

public class GetPublishedCoursesListQueryHandler : ResponseHandler,
    IRequestHandler<GetPublishedCoursesListQuery, Response<List<CourseCatalogIndexItemDto>>>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly IMapper _mapper;
    private readonly IMediaUrlResolver _mediaUrlResolver;

    public GetPublishedCoursesListQueryHandler(
        ICourseRepository courseRepository,
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        IMapper mapper,
        IMediaUrlResolver mediaUrlResolver,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _courseRepository = courseRepository;
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _mapper = mapper;
        _mediaUrlResolver = mediaUrlResolver;
    }

    public async Task<Response<List<CourseCatalogIndexItemDto>>> Handle(
        GetPublishedCoursesListQuery request,
        CancellationToken cancellationToken)
    {
        // When a guardian browses on behalf of a specific child, validate the link.
        // The student's profile is NOT used to narrow the catalog - only the
        // explicit query filters are applied. If no filter is sent, all published
        // courses are returned.
        if (request.StudentId.HasValue)
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId.Value);
            if (student == null)
                return NotFound<List<CourseCatalogIndexItemDto>>("Student not found.");

            var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
            if (guardian == null || student.GuardianId != guardian.Id)
                return Forbidden<List<CourseCatalogIndexItemDto>>(
                    "You don't have permission to browse courses for this student.");
        }

        var query = _courseRepository.GetPublishedCoursesQueryable();

        if (request.DomainId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.Subject != null && c.TeacherSubject.Subject.DomainId == request.DomainId.Value);
        if (request.CurriculumId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.Subject != null && c.TeacherSubject.Subject.CurriculumId == request.CurriculumId.Value);
        if (request.LevelId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.Subject != null && c.TeacherSubject.Subject.LevelId == request.LevelId.Value);
        if (request.GradeId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.Subject != null && c.TeacherSubject.Subject.GradeId == request.GradeId.Value);
        if (request.SubjectId.HasValue)
            query = query.Where(c => c.TeacherSubject != null && c.TeacherSubject.SubjectId == request.SubjectId.Value);
        if (request.TeachingModeId.HasValue)
            query = query.Where(c => c.TeachingModeId == request.TeachingModeId.Value);
        if (request.TeacherId.HasValue)
            query = query.Where(c => c.TeacherId == request.TeacherId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        // Closed-over constant so EF can translate the ternary (GetLocalizedValue is not SQL-translatable).
        var isAr = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            .Equals("ar", StringComparison.OrdinalIgnoreCase);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseCatalogIndexItemDto
            {
                Id = c.Id,
                Title = c.Title,
                ImageUrl = c.ImageUrl,
                Price = c.Price,
                TeacherDisplayName = c.Teacher != null && c.Teacher.User != null
                    ? (c.Teacher.User.FirstName + " " + c.Teacher.User.LastName).Trim()
                    : null,
                TeacherAverageReview = c.Teacher != null
                    ? (c.Teacher.TeacherReviews
                          .Where(r => r.IsApproved)
                          .Select(r => (decimal?)r.Rating)
                          .Average() ?? 0m)
                    : 0m,

                DomainName = c.TeacherSubject != null &&
                             c.TeacherSubject.Subject != null &&
                             c.TeacherSubject.Subject.Domain != null
                    ? (isAr
                        ? c.TeacherSubject.Subject.Domain.NameAr
                        : c.TeacherSubject.Subject.Domain.NameEn)
                    : null,

                SubjectName = c.TeacherSubject != null && c.TeacherSubject.Subject != null
                    ? (isAr
                        ? c.TeacherSubject.Subject.NameAr
                        : c.TeacherSubject.Subject.NameEn)
                    : null,

                CurriculumName = c.TeacherSubject != null &&
                                 c.TeacherSubject.Subject != null &&
                                 c.TeacherSubject.Subject.Curriculum != null
                    ? (isAr
                        ? c.TeacherSubject.Subject.Curriculum.NameAr
                        : c.TeacherSubject.Subject.Curriculum.NameEn)
                    : null,

                LevelName = c.TeacherSubject != null &&
                            c.TeacherSubject.Subject != null &&
                            c.TeacherSubject.Subject.Level != null
                    ? (isAr
                        ? c.TeacherSubject.Subject.Level.NameAr
                        : c.TeacherSubject.Subject.Level.NameEn)
                    : null,

                GradeName = c.TeacherSubject != null &&
                            c.TeacherSubject.Subject != null &&
                            c.TeacherSubject.Subject.Grade != null
                    ? (isAr
                        ? c.TeacherSubject.Subject.Grade.NameAr
                        : c.TeacherSubject.Subject.Grade.NameEn)
                    : null,

                TeachingModeName = c.TeachingMode != null
                    ? (isAr ? c.TeachingMode.NameAr : c.TeachingMode.NameEn)
                    : null,

                SessionTypeName = c.SessionType != null
                    ? (isAr ? c.SessionType.NameAr : c.SessionType.NameEn)
                    : null,

                SessionsCount = c.IsFlexible ? null : c.Sessions.Count,
                SessionDurationMinutes = c.SessionDurationMinutes,
                TotalDurationMinutes = !c.IsFlexible && c.SessionDurationMinutes.HasValue
                    ? (int?)(c.Sessions.Count * c.SessionDurationMinutes.Value)
                    : null
            })
            .ToListAsync(cancellationToken);

        foreach (var item in items)
            item.ImageUrl = _mediaUrlResolver.ToPublicUrl(item.ImageUrl);

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
