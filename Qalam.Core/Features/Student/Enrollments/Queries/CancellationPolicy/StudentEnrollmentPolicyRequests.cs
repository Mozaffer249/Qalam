using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Policy;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Core.Features.Student.Enrollments.Queries.CancellationPolicy;

public class GetEnrollmentCancelPreviewQuery : IRequest<Response<PolicyPreviewDto>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
    public int EnrollmentId { get; set; }
}

public class GetEnrollmentCancellationPolicyQuery : IRequest<Response<PolicySummaryDto>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
    public int EnrollmentId { get; set; }
}

public class StudentEnrollmentPolicyHandlers : ResponseHandler,
    IRequestHandler<GetEnrollmentCancelPreviewQuery, Response<PolicyPreviewDto>>,
    IRequestHandler<GetEnrollmentCancellationPolicyQuery, Response<PolicySummaryDto>>
{
    private readonly IEnrollmentRepository _enrollments;
    private readonly IEnrollmentCancellationService _cancellation;
    private readonly IPolicyResolver _resolver;

    public StudentEnrollmentPolicyHandlers(
        IEnrollmentRepository enrollments,
        IEnrollmentCancellationService cancellation,
        IPolicyResolver resolver,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollments = enrollments;
        _cancellation = cancellation;
        _resolver = resolver;
    }

    public async Task<Response<PolicyPreviewDto>> Handle(GetEnrollmentCancelPreviewQuery request, CancellationToken cancellationToken)
    {
        var owner = await _enrollments.GetTableNoTracking()
            .Where(e => e.Id == request.EnrollmentId)
            .Select(e => e.OwnerUserId ?? (e.EnrollmentRequest != null ? (int?)e.EnrollmentRequest.RequestedByUserId : null))
            .FirstOrDefaultAsync(cancellationToken);
        if (owner != request.UserId)
            return NotFound<PolicyPreviewDto>("Enrollment not found.");

        var preview = await _cancellation.PreviewAsync(request.EnrollmentId, cancellationToken);
        return preview == null ? NotFound<PolicyPreviewDto>("Enrollment not found.") : Success(entity: preview);
    }

    public async Task<Response<PolicySummaryDto>> Handle(GetEnrollmentCancellationPolicyQuery request, CancellationToken cancellationToken)
    {
        var enrollment = await _enrollments.GetTableNoTracking()
            .Include(e => e.EnrollmentRequest)
            .Include(e => e.PolicyVersion)
            .Include(e => e.Participants).ThenInclude(p => p.Student)
            .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId, cancellationToken);
        if (enrollment == null)
            return NotFound<PolicySummaryDto>("Enrollment not found.");

        var allowed = enrollment.OwnerUserId == request.UserId
                      || enrollment.EnrollmentRequest?.RequestedByUserId == request.UserId
                      || enrollment.Participants.Any(p => p.Student != null && p.Student.UserId == request.UserId);
        if (!allowed)
            return NotFound<PolicySummaryDto>("Enrollment not found.");

        var policy = await _resolver.ForEnrollmentAsync(enrollment, cancellationToken);
        return Success(entity: PolicySummaryBuilder.ForStudent(policy, enrollment.PolicyVersion?.EffectiveFrom));
    }
}
