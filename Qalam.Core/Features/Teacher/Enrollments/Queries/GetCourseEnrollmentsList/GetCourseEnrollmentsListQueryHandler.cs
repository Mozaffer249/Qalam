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
    private readonly IEnrollmentRepository _enrollmentRepository;

    public GetCourseEnrollmentsListQueryHandler(
        ITeacherRepository teacherRepository,
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
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

        var query = _enrollmentRepository.GetTableNoTracking()
            .Include(e => e.LeaderStudent).ThenInclude(s => s!.User)
            .Include(e => e.Participants).ThenInclude(p => p.Student).ThenInclude(s => s.User)
            .Include(e => e.CourseSchedules)
            .Where(e => e.CourseId == request.CourseId);

        if (request.Status.HasValue)
            query = query.Where(e => e.EnrollmentStatus == request.Status.Value);

        var enrollments = await query
            .OrderByDescending(e => e.ActivatedAt ?? e.ApprovedAt)
            .ToListAsync(cancellationToken);

        var totalCount = enrollments.Count;

        var page = enrollments
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e =>
            {
                string displayName;
                if (e.Kind == EnrollmentKind.Individual)
                {
                    var only = e.Participants.FirstOrDefault();
                    var student = only?.Student;
                    displayName = student?.User != null
                        ? (student.User.FirstName + " " + student.User.LastName).Trim()
                        : $"Student #{only?.StudentId ?? 0}";
                }
                else
                {
                    var leaderName = e.LeaderStudent?.User != null
                        ? (e.LeaderStudent.User.FirstName + " " + e.LeaderStudent.User.LastName).Trim()
                        : $"Student #{e.LeaderStudentId}";
                    displayName = $"Group of {e.Participants.Count} — Leader: {leaderName}";
                }

                return new TeacherEnrollmentListItemDto
                {
                    Id = e.Id,
                    Kind = e.Kind,
                    CourseId = e.CourseId,
                    CourseTitle = course.Title,
                    DisplayName = displayName,
                    EnrollmentStatus = e.EnrollmentStatus,
                    ApprovedAt = e.ApprovedAt,
                    ActivatedAt = e.ActivatedAt,
                    PaymentDeadline = e.PaymentDeadline,
                    ParticipantCount = e.Participants.Count,
                    PaidParticipantCount = e.Participants.Count(p => p.PaymentStatus == PaymentStatus.Succeeded),
                    SessionsCount = e.CourseSchedules.Count
                };
            })
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
