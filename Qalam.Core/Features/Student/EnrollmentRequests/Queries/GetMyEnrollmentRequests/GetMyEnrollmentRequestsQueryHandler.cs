using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyEnrollmentRequests;

public class GetMyEnrollmentRequestsQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentRequestsQuery, Response<PaginatedResult<EnrollmentRequestListItemDto>>>
{
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly IMapper _mapper;

    public GetMyEnrollmentRequestsQueryHandler(
        ICourseEnrollmentRequestRepository requestRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _requestRepository = requestRepository;
        _mapper = mapper;
    }

    public async Task<Response<PaginatedResult<EnrollmentRequestListItemDto>>> Handle(
        GetMyEnrollmentRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _requestRepository.GetTableNoTracking()
            .Include(r => r.Course).ThenInclude(c => c.TeachingMode)
            .Include(r => r.Course).ThenInclude(c => c.SessionType)
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

        var result = new PaginatedResult<EnrollmentRequestListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
        return Success(entity: result);
    }
}
