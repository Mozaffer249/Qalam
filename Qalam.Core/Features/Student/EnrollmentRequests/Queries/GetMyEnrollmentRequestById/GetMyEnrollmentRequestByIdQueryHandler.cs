using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyEnrollmentRequestById;

public class GetMyEnrollmentRequestByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentRequestByIdQuery, Response<EnrollmentRequestDetailDto>>
{
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly IMapper _mapper;

    public GetMyEnrollmentRequestByIdQueryHandler(
        ICourseEnrollmentRequestRepository requestRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _requestRepository = requestRepository;
        _mapper = mapper;
    }

    public async Task<Response<EnrollmentRequestDetailDto>> Handle(
        GetMyEnrollmentRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var enrollmentRequest = await _requestRepository.GetTableNoTracking()
            .Include(r => r.Course)
                .ThenInclude(c => c.TeachingMode)
            .Include(r => r.Course)
                .ThenInclude(c => c.SessionType)
            .Include(r => r.SelectedAvailabilities)
            .Include(r => r.GroupMembers)
            .Include(r => r.ProposedSessions)
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.RequestedByUserId == request.UserId, cancellationToken);

        if (enrollmentRequest == null)
            return NotFound<EnrollmentRequestDetailDto>("Enrollment request not found.");

        var dto = _mapper.Map<EnrollmentRequestDetailDto>(enrollmentRequest);

        return Success(entity: dto);
    }
}
