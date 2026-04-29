using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.CourseCatalog.Queries.GetRecommendedCourses;

public class GetRecommendedCoursesQueryHandler : ResponseHandler,
    IRequestHandler<GetRecommendedCoursesQuery, Response<List<CourseCatalogItemDto>>>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly IMapper _mapper;

    public GetRecommendedCoursesQueryHandler(
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
        GetRecommendedCoursesQuery request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByIdAsync(request.StudentId);
        if (student == null)
            return NotFound<List<CourseCatalogItemDto>>("Student not found.");

        var isSelf = student.UserId == request.UserId;
        if (!isSelf)
        {
            var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
            if (guardian == null || student.GuardianId != guardian.Id)
                return Forbidden<List<CourseCatalogItemDto>>("You don't have permission to browse courses for this student.");
        }

        var query = _courseRepository.GetPublishedCoursesQueryable();

        // Domain-based recommendations only
        if (student.DomainId.HasValue)
        {
            var domainId = student.DomainId.Value;
            query = query.Where(c =>
                c.TeacherSubject != null &&
                c.TeacherSubject.Subject != null &&
                c.TeacherSubject.Subject.DomainId == domainId);
        }

        var items = await query
            .Take(4)
            .Select(c => new CourseCatalogItemDto
            {
                Id = c.Id,
                Title = c.Title,
                DescriptionShort = c.Description != null && c.Description.Length > 200
                    ? c.Description.Substring(0, 200) + "..."
                    : c.Description,
                TeacherDisplayName = c.Teacher != null && c.Teacher.User != null
                    ? (c.Teacher.User.FirstName + " " + c.Teacher.User.LastName).Trim()
                    : null,
                TeacherBio = c.Teacher != null ? c.Teacher.Bio : null,
                TeacherAverageReview = c.Teacher != null
                    ? (c.Teacher.TeacherReviews
                          .Where(r => r.IsApproved)
                          .Select(r => (decimal?)r.Rating)
                          .Average() ?? 0m)
                    : 0m,
                EnrollmentsCount = c.CourseEnrollments.Count(e =>
                    e.EnrollmentStatus == EnrollmentStatus.Active ||
                    e.EnrollmentStatus == EnrollmentStatus.Completed),
                DomainId = c.TeacherSubject != null && c.TeacherSubject.Subject != null
                    ? c.TeacherSubject.Subject.DomainId
                    : 0,
                DomainNameEn = c.TeacherSubject != null &&
                               c.TeacherSubject.Subject != null &&
                               c.TeacherSubject.Subject.Domain != null
                    ? c.TeacherSubject.Subject.Domain.NameEn
                    : null,
                SubjectId = c.TeacherSubject != null ? c.TeacherSubject.SubjectId : 0,
                SubjectNameEn = c.TeacherSubject != null && c.TeacherSubject.Subject != null
                    ? c.TeacherSubject.Subject.NameEn
                    : null,
                TeachingModeId = c.TeachingModeId,
                TeachingModeNameEn = c.TeachingMode != null ? c.TeachingMode.NameEn : null,
                SessionTypeId = c.SessionTypeId,
                SessionTypeNameEn = c.SessionType != null ? c.SessionType.NameEn : null,
                Price = c.Price,
                MaxStudents = c.MaxStudents,
                AvailableSeats = c.MaxStudents.HasValue
                    ? c.MaxStudents.Value - c.CourseEnrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
                    : (int?)null,
                IsFlexible = c.IsFlexible,
                SessionsCount = c.SessionsCount,
                SessionDurationMinutes = c.SessionDurationMinutes
            })
            .ToListAsync(cancellationToken);
        return Success(entity: items);
    }
}

