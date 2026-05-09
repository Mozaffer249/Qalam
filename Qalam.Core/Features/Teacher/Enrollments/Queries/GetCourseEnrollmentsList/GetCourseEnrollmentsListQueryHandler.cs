using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetCourseEnrollmentsList;

public class GetCourseEnrollmentsListQueryHandler : ResponseHandler,
    IRequestHandler<GetCourseEnrollmentsListQuery, Response<List<TeacherEnrollmentListItemDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseEnrollmentRepository _enrollmentRepository;
    private readonly ICourseGroupEnrollmentRepository _groupEnrollmentRepository;

    public GetCourseEnrollmentsListQueryHandler(
        ITeacherRepository teacherRepository,
        ICourseRepository courseRepository,
        ICourseEnrollmentRepository enrollmentRepository,
        ICourseGroupEnrollmentRepository groupEnrollmentRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
        _groupEnrollmentRepository = groupEnrollmentRepository;
    }

    public async Task<Response<List<TeacherEnrollmentListItemDto>>> Handle(
        GetCourseEnrollmentsListQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<List<TeacherEnrollmentListItemDto>>("Teacher profile not found.");

        var course = await _courseRepository.GetByIdAsync(request.CourseId);
        if (course == null || course.TeacherId != teacher.Id)
            return NotFound<List<TeacherEnrollmentListItemDto>>("Course not found or does not belong to you.");

        // ── Individual enrollments
        var indQuery = _enrollmentRepository.GetTableNoTracking()
            .Include(e => e.Student).ThenInclude(s => s.User)
            .Include(e => e.CourseSchedules)
            .Include(e => e.CourseEnrollmentPayments)
            .Where(e => e.CourseId == request.CourseId);

        if (request.Status.HasValue)
            indQuery = indQuery.Where(e => e.EnrollmentStatus == request.Status.Value);

        var individualRows = await indQuery
            .OrderByDescending(e => e.ApprovedAt)
            .ToListAsync(cancellationToken);

        // ── Group enrollments
        var grpQuery = _groupEnrollmentRepository.GetTableNoTracking()
            .Include(g => g.LeaderStudent).ThenInclude(s => s.User)
            .Include(g => g.Members)
            .Include(g => g.CourseSchedules)
            .Where(g => g.CourseId == request.CourseId);

        if (request.Status.HasValue)
            grpQuery = grpQuery.Where(g => g.Status == request.Status.Value);

        var groupRows = await grpQuery
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

        // ── Merge into a single mixed list, sorted by most recent activity
        var items = new List<TeacherEnrollmentListItemDto>(individualRows.Count + groupRows.Count);

        foreach (var e in individualRows)
        {
            var paidCount = e.CourseEnrollmentPayments.Count(p => p.Status == PaymentStatus.Succeeded);
            items.Add(new TeacherEnrollmentListItemDto
            {
                Id = e.Id,
                Kind = TeacherEnrollmentKind.Individual,
                CourseId = e.CourseId,
                CourseTitle = course.Title,
                DisplayName = e.Student?.User != null
                    ? (e.Student.User.FirstName + " " + e.Student.User.LastName).Trim()
                    : $"Student #{e.StudentId}",
                EnrollmentStatus = e.EnrollmentStatus,
                ApprovedAt = e.ApprovedAt,
                ActivatedAt = e.ActivatedAt,
                PaymentDeadline = e.PaymentDeadline,
                MemberCount = 1,
                PaidMemberCount = paidCount > 0 ? 1 : 0,
                SessionsCount = e.CourseSchedules.Count
            });
        }

        foreach (var g in groupRows)
        {
            var leaderName = g.LeaderStudent?.User != null
                ? (g.LeaderStudent.User.FirstName + " " + g.LeaderStudent.User.LastName).Trim()
                : $"Student #{g.LeaderStudentId}";

            items.Add(new TeacherEnrollmentListItemDto
            {
                Id = g.Id,
                Kind = TeacherEnrollmentKind.Group,
                CourseId = g.CourseId,
                CourseTitle = course.Title,
                DisplayName = $"Group of {g.Members.Count} — Leader: {leaderName}",
                EnrollmentStatus = g.Status,
                ApprovedAt = g.CreatedAt,
                ActivatedAt = g.ActivatedAt,
                PaymentDeadline = g.PaymentDeadline,
                MemberCount = g.Members.Count,
                PaidMemberCount = g.Members.Count(m => m.PaymentStatus == PaymentStatus.Succeeded),
                SessionsCount = g.CourseSchedules.Count
            });
        }

        var ordered = items
            .OrderByDescending(i => i.ActivatedAt ?? i.ApprovedAt)
            .ToList();

        var totalCount = ordered.Count;
        var page = ordered
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

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

        return Success(entity: page, Meta: meta);
    }
}
