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
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly IMapper _mapper;

    public GetMyEnrollmentRequestsQueryHandler(
        IStudentRepository studentRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _requestRepository = requestRepository;
        _mapper = mapper;
    }

    public async Task<Response<PaginatedResult<EnrollmentRequestListItemDto>>> Handle(
        GetMyEnrollmentRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student == null)
            return NotFound<PaginatedResult<EnrollmentRequestListItemDto>>("Student not found.");

        var query = _requestRepository.GetByStudentIdQueryable(student.Id);
        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);

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
