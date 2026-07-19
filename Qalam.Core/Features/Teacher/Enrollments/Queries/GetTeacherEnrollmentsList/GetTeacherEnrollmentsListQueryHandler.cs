using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetTeacherEnrollmentsList;

public class GetTeacherEnrollmentsListQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherEnrollmentsListQuery, Response<List<TeacherEnrollmentListItemDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly PaymentSettings _paymentSettings;

    public GetTeacherEnrollmentsListQueryHandler(
        ITeacherRepository teacherRepository,
        IEnrollmentRepository enrollmentRepository,
        IOptions<PaymentSettings> paymentSettings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _enrollmentRepository = enrollmentRepository;
        _paymentSettings = paymentSettings.Value;
    }

    public async Task<Response<List<TeacherEnrollmentListItemDto>>> Handle(
        GetTeacherEnrollmentsListQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<List<TeacherEnrollmentListItemDto>>("Teacher profile not found.");

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
            .Where(e => e.ApprovedByTeacherId == teacher.Id);

        if (request.Status.HasValue)
            query = query.Where(e => e.EnrollmentStatus == request.Status.Value);

        if (request.Source.HasValue)
            query = query.Where(e => e.Source == request.Source.Value);

        if (request.Kind.HasValue)
            query = query.Where(e => e.Kind == request.Kind.Value);

        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(e =>
                (e.Course != null && EF.Functions.Like(e.Course.Title, pattern))
                || (e.OpenSessionRequest != null
                    && e.OpenSessionRequest.Subject != null
                    && (EF.Functions.Like(e.OpenSessionRequest.Subject.NameEn, pattern)
                        || (e.OpenSessionRequest.Subject.NameAr != null
                            && EF.Functions.Like(e.OpenSessionRequest.Subject.NameAr, pattern))))
                || e.Participants.Any(p =>
                    p.Student != null
                    && p.Student.User != null
                    && (EF.Functions.Like(p.Student.User.FirstName, pattern)
                        || EF.Functions.Like(p.Student.User.LastName, pattern)
                        || EF.Functions.Like(
                            p.Student.User.FirstName + " " + p.Student.User.LastName,
                            pattern)))
                || (e.LeaderStudent != null
                    && e.LeaderStudent.User != null
                    && (EF.Functions.Like(e.LeaderStudent.User.FirstName, pattern)
                        || EF.Functions.Like(e.LeaderStudent.User.LastName, pattern))));
        }

        var enrollments = await query
            .OrderByDescending(e => e.ActivatedAt ?? e.ApprovedAt)
            .ToListAsync(cancellationToken);

        var currency = _paymentSettings.DefaultCurrency;
        var mapped = enrollments
            .Select(e => TeacherEnrollmentMapping.ToListItem(e, currency))
            .ToList();

        if (request.SourceBadge.HasValue)
            mapped = mapped.Where(x => x.SourceBadge == request.SourceBadge.Value).ToList();

        var totalCount = mapped.Count;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var page = mapped
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var totalPages = pageSize > 0
            ? (int)Math.Ceiling(totalCount / (double)pageSize)
            : 0;

        var meta = new
        {
            totalCount,
            pageNumber,
            pageSize,
            totalPages,
            hasPreviousPage = pageNumber > 1,
            hasNextPage = pageNumber < totalPages
        };

        return Success(entity: page, Meta: meta);
    }
}
