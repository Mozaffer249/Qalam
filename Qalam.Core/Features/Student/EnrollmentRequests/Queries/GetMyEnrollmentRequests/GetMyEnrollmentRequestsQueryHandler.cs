using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyEnrollmentRequests;

public class GetMyEnrollmentRequestsQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentRequestsQuery, Response<List<EnrollmentRequestListItemDto>>>
{
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMapper _mapper;

    public GetMyEnrollmentRequestsQueryHandler(
        ICourseEnrollmentRequestRepository requestRepository,
        IEnrollmentRepository enrollmentRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _requestRepository = requestRepository;
        _enrollmentRepository = enrollmentRepository;
        _mapper = mapper;
    }

    public async Task<Response<List<EnrollmentRequestListItemDto>>> Handle(
        GetMyEnrollmentRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _requestRepository.GetTableNoTracking()
            .Include(r => r.Course).ThenInclude(c => c.TeachingMode)
            .Include(r => r.Course).ThenInclude(c => c.SessionType)
            .Include(r => r.Course).ThenInclude(c => c.Teacher).ThenInclude(t => t.User)
            .Include(r => r.Course).ThenInclude(c => c.TeacherSubject).ThenInclude(ts => ts.Subject)
            .Include(r => r.Course).ThenInclude(c => c.Sessions)
            .Include(r => r.GroupMembers)
            .Where(r => r.RequestedByUserId == request.UserId)
            .OrderByDescending(r => r.CreatedAt);

        if (request.Status.HasValue)
            query = (IOrderedQueryable<Data.Entity.Course.CourseEnrollmentRequest>)
                query.Where(r => r.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var requests = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = _mapper.Map<List<EnrollmentRequestListItemDto>>(requests);

        var requestIds = requests.Select(r => r.Id).ToList();
        var enrollments = await _enrollmentRepository.GetTableNoTracking()
            .Where(e => e.EnrollmentRequestId != null
                        && requestIds.Contains(e.EnrollmentRequestId.Value))
            .Select(e => new
            {
                e.Id,
                e.EnrollmentRequestId,
                e.EnrollmentStatus
            })
            .ToListAsync(cancellationToken);

        var enrollmentByRequestId = enrollments
            .GroupBy(e => e.EnrollmentRequestId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        for (var i = 0; i < items.Count; i++)
        {
            var entity = requests[i];
            var item = items[i];
            item.HasPendingInvites = entity.GroupMembers.Any(gm =>
                gm.MemberType == GroupMemberType.Invited
                && gm.ConfirmationStatus == GroupMemberConfirmationStatus.Pending);

            var isGroup = string.Equals(
                entity.Course?.SessionType?.Code,
                "group",
                StringComparison.OrdinalIgnoreCase);
            item.Kind = isGroup ? EnrollmentKind.Group : EnrollmentKind.Individual;

            if (enrollmentByRequestId.TryGetValue(entity.Id, out var enrollment))
            {
                item.EnrollmentId = enrollment.Id;
                item.EnrollmentStatus = enrollment.EnrollmentStatus;
            }
        }

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
