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
using Qalam.Service.Implementations;
using System.Globalization;
using StudentProfile = Qalam.Data.Entity.Student.Student;

namespace Qalam.Core.Features.Student.CourseCatalog.Queries.GetRecommendedCourses;

public class GetRecommendedCoursesQueryHandler : ResponseHandler,
    IRequestHandler<GetRecommendedCoursesQuery, Response<List<CourseCatalogItemDto>>>
{
    private const int DefaultTake = 4;

    private readonly ICourseRepository _courseRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly IMapper _mapper;
    private readonly IFreeSessionPolicyService _freeSessionPolicy;

    public GetRecommendedCoursesQueryHandler(
        ICourseRepository courseRepository,
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        IMapper mapper,
        IFreeSessionPolicyService freeSessionPolicy,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _courseRepository = courseRepository;
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _mapper = mapper;
        _freeSessionPolicy = freeSessionPolicy;
    }

    public async Task<Response<List<CourseCatalogItemDto>>> Handle(
        GetRecommendedCoursesQuery request,
        CancellationToken cancellationToken)
    {
        HashSet<int> domainIds;

        if (request.StudentId <= 0)
        {
            domainIds = await ResolveHouseholdDomainIdsAsync(request.UserId, cancellationToken);
        }
        else
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId);
            if (student == null)
                return NotFound<List<CourseCatalogItemDto>>("Student not found.");

            var isSelf = student.UserId == request.UserId;
            if (!isSelf)
            {
                var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
                if (guardian == null || student.GuardianId != guardian.Id)
                    return Forbidden<List<CourseCatalogItemDto>>(
                        "You don't have permission to browse courses for this student.");
            }

            domainIds = student.DomainId.HasValue
                ? new HashSet<int> { student.DomainId.Value }
                : new HashSet<int>();
        }

        var query = _courseRepository.GetPublishedCoursesQueryable();

        if (domainIds.Count > 0)
        {
            query = query.Where(c =>
                c.TeacherSubject != null &&
                c.TeacherSubject.Subject != null &&
                domainIds.Contains(c.TeacherSubject.Subject.DomainId));
        }

        var isAr = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            .Equals("ar", StringComparison.OrdinalIgnoreCase);

        var rows = await query
            .Take(DefaultTake)
            .Select(c => new
            {
                Item = new CourseCatalogItemDto
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
                    EnrollmentsCount = c.Enrollments.Count(e =>
                        e.EnrollmentStatus == EnrollmentStatus.Active ||
                        e.EnrollmentStatus == EnrollmentStatus.Completed),
                    DomainId = c.TeacherSubject != null && c.TeacherSubject.Subject != null
                        ? c.TeacherSubject.Subject.DomainId
                        : 0,
                    DomainName = c.TeacherSubject != null &&
                                 c.TeacherSubject.Subject != null &&
                                 c.TeacherSubject.Subject.Domain != null
                        ? (isAr
                            ? c.TeacherSubject.Subject.Domain.NameAr
                            : c.TeacherSubject.Subject.Domain.NameEn)
                        : null,
                    SubjectId = c.TeacherSubject != null ? c.TeacherSubject.SubjectId : 0,
                    SubjectName = c.TeacherSubject != null && c.TeacherSubject.Subject != null
                        ? (isAr
                            ? c.TeacherSubject.Subject.NameAr
                            : c.TeacherSubject.Subject.NameEn)
                        : null,
                    TeachingModeId = c.TeachingModeId,
                    TeachingModeName = c.TeachingMode != null
                        ? (isAr ? c.TeachingMode.NameAr : c.TeachingMode.NameEn)
                        : null,
                    SessionTypeId = c.SessionTypeId,
                    SessionTypeName = c.SessionType != null
                        ? (isAr ? c.SessionType.NameAr : c.SessionType.NameEn)
                        : null,
                    Price = c.Price,
                    MaxStudents = c.MaxStudents,
                    AvailableSeats = c.MaxStudents.HasValue
                        ? c.MaxStudents.Value - c.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
                        : (int?)null,
                    IsFlexible = c.IsFlexible,
                    SessionsCount = c.SessionsCount,
                    SessionDurationMinutes = c.SessionDurationMinutes
                },
                SessionTypeCode = c.SessionType != null ? c.SessionType.Code : PricingDefaults.SessionTypeIndividual,
                FirstSessionDurationMinutes = !c.IsFlexible && c.Sessions.Any()
                    ? (int?)c.Sessions
                        .OrderBy(s => s.SessionNumber)
                        .Select(s => s.DurationMinutes)
                        .FirstOrDefault()
                    : null,
                TotalDurationMinutes = !c.IsFlexible
                    ? (c.Sessions.Any()
                        ? (int?)c.Sessions.Sum(s => s.DurationMinutes)
                        : (c.SessionDurationMinutes.HasValue && (c.SessionsCount ?? 0) > 0
                            ? (int?)(c.SessionsCount!.Value * c.SessionDurationMinutes.Value)
                            : null))
                    : null
            })
            .ToListAsync(cancellationToken);

        var unusedTrial = false;
        if (request.StudentId > 0)
        {
            unusedTrial = await _freeSessionPolicy.IsStudentEligibleForFreeTrialAsync(
                request.StudentId, cancellationToken);
        }
        else
        {
            var viewerStudent = await _studentRepository.GetByUserIdAsync(request.UserId);
            if (viewerStudent != null)
            {
                unusedTrial = await _freeSessionPolicy.IsStudentEligibleForFreeTrialAsync(
                    viewerStudent.Id, cancellationToken);
            }
        }

        var items = rows.Select(r =>
        {
            var isGroup = string.Equals(
                r.SessionTypeCode,
                PricingDefaults.SessionTypeGroup,
                StringComparison.OrdinalIgnoreCase);
            var eligible = unusedTrial
                && _freeSessionPolicy.IsEligiblePackage(isGroup, r.Item.SessionsCount ?? 0);
            r.Item.IsFreeTrialEligible = eligible;

            var totalMinutes = r.TotalDurationMinutes ?? 0;
            var firstMinutes = FreeSessionPolicyService.ResolveFirstSessionMinutes(
                r.FirstSessionDurationMinutes,
                r.Item.SessionDurationMinutes,
                r.TotalDurationMinutes,
                r.Item.SessionsCount);
            var hourly = FreeSessionPolicyService.DerivePricePerHour(r.Item.Price, totalMinutes);
            var (credit, amountDue) = FreeSessionPolicyService.BuildTeaserAmounts(
                eligible, r.Item.Price, hourly, firstMinutes);
            r.Item.FreeSessionCredit = credit;
            r.Item.AmountDue = amountDue;
            return r.Item;
        }).ToList();

        return Success(entity: items);
    }

    private async Task<HashSet<int>> ResolveHouseholdDomainIdsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var domainIds = new HashSet<int>();
        var ownStudent = await _studentRepository.GetByUserIdAsync(userId);
        if (ownStudent?.DomainId is int ownDomain)
            domainIds.Add(ownDomain);

        var guardian = await _guardianRepository.GetByUserIdAsync(userId);
        if (guardian != null)
        {
            var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);
            foreach (var child in children)
            {
                if (child.DomainId is int childDomain)
                    domainIds.Add(childDomain);
            }
        }

        return domainIds;
    }
}
