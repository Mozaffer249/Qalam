using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.RevokeTeacherDomainApproval;

public class RevokeTeacherDomainApprovalCommandHandler : ResponseHandler,
    IRequestHandler<RevokeTeacherDomainApprovalCommand, Response<string>>
{
    private readonly ITeacherDomainApprovalService _domainApprovalService;
    private readonly ITeacherRegistrationCompletionService _completionService;

    public RevokeTeacherDomainApprovalCommandHandler(
        ITeacherDomainApprovalService domainApprovalService,
        ITeacherRegistrationCompletionService completionService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _domainApprovalService = domainApprovalService;
        _completionService = completionService;
    }

    public async Task<Response<string>> Handle(
        RevokeTeacherDomainApprovalCommand request,
        CancellationToken cancellationToken)
    {
        var (success, error) = await _domainApprovalService.RevokeAsync(
            request.TeacherId,
            request.DomainId,
            request.UserId,
            request.Reason,
            cancellationToken);

        if (!success)
            return BadRequest<string>(error ?? "Unable to revoke domain approval.");

        await _completionService.RefreshTeacherStatusAfterReviewAsync(request.TeacherId, cancellationToken);

        return Success<string>("Education domain approval revoked");
    }
}
