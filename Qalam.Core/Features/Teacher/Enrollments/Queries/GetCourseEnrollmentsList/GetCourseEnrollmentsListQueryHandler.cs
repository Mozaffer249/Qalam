using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetCourseEnrollmentsList;

public class GetCourseEnrollmentsListQueryHandler : ResponseHandler,
    IRequestHandler<GetCourseEnrollmentsListQuery, Response<List<TeacherEnrollmentListItemDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly PaymentSettings _paymentSettings;

    public GetCourseEnrollmentsListQueryHandler(
        ITeacherRepository teacherRepository,
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository,
        IOptions<PaymentSettings> paymentSettings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
        _paymentSettings = paymentSettings.Value;
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
            .Include(e => e.Course!).ThenInclude(c => c.TeachingMode)
            .Include(e => e.Course!).ThenInclude(c => c.SessionType)
            .Include(e => e.Course!).ThenInclude(c => c.TeacherSubject).ThenInclude(ts => ts.Subject)
            .Include(e => e.LeaderStudent).ThenInclude(s => s!.User)
            .Include(e => e.Participants).ThenInclude(p => p.Student).ThenInclude(s => s.User)
            .Include(e => e.CourseSchedules)
            .Include(e => e.EnrollmentRequest)
            .Include(e => e.OpenSessionRequest!).ThenInclude(r => r.Subject)
            .Include(e => e.OpenSessionRequest!).ThenInclude(r => r.TeachingMode)
            .Where(e => e.CourseId == request.CourseId);

        if (request.Status.HasValue)
            query = query.Where(e => e.EnrollmentStatus == request.Status.Value);

        var enrollments = await query
            .OrderByDescending(e => e.ActivatedAt ?? e.ApprovedAt)
            .ToListAsync(cancellationToken);

        var totalCount = enrollments.Count;
        var currency = _paymentSettings.DefaultCurrency;

        var page = enrollments
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => TeacherEnrollmentMapping.ToListItem(e, currency))
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
