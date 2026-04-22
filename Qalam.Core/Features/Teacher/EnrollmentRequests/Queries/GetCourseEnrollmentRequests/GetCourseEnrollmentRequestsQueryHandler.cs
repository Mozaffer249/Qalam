using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.EnrollmentRequests.Queries.GetCourseEnrollmentRequests;

public class GetCourseEnrollmentRequestsQueryHandler : ResponseHandler,
    IRequestHandler<GetCourseEnrollmentRequestsQuery, Response<List<TeacherEnrollmentRequestListItemDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseEnrollmentRequestRepository _requestRepository;

    public GetCourseEnrollmentRequestsQueryHandler(
        ITeacherRepository teacherRepository,
        ICourseRepository courseRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _courseRepository = courseRepository;
        _requestRepository = requestRepository;
    }

    public async Task<Response<List<TeacherEnrollmentRequestListItemDto>>> Handle(
        GetCourseEnrollmentRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<List<TeacherEnrollmentRequestListItemDto>>("Teacher profile not found.");

        var course = await _courseRepository.GetByIdAsync(request.CourseId);
        if (course == null || course.TeacherId != teacher.Id)
            return NotFound<List<TeacherEnrollmentRequestListItemDto>>("Course not found or does not belong to you.");

        var query = _requestRepository.GetByCourseIdQueryable(request.CourseId);

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value).OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var requests = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = requests.Select(r => new TeacherEnrollmentRequestListItemDto
        {
            Id = r.Id,
            CourseId = r.CourseId,
            CourseTitle = r.Course?.Title ?? "",
            RequestedByUserName = r.RequestedByUser != null
                ? (r.RequestedByUser.FirstName + " " + r.RequestedByUser.LastName).Trim()
                : null,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            TotalMinutes = r.TotalMinutes,
            EstimatedTotalPrice = r.EstimatedTotalPrice,
            GroupMemberCount = r.GroupMembers.Count,
            TeachingModeNameEn = r.Course?.TeachingMode?.NameEn,
            SessionTypeNameEn = r.Course?.SessionType?.NameEn
        }).ToList();

        return Success(
            entity: items,
            Meta: BuildPaginationMeta(request.PageNumber, request.PageSize, totalCount));
    }
}
