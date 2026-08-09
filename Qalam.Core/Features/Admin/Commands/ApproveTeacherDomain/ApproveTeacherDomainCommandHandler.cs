using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.ApproveTeacherDomain;

public class ApproveTeacherDomainCommandHandler : ResponseHandler,
    IRequestHandler<ApproveTeacherDomainCommand, Response<string>>
{
    private readonly ITeacherDomainApprovalService _domainApprovalService;
    private readonly ITeacherRegistrationCompletionService _completionService;

    public ApproveTeacherDomainCommandHandler(
        ITeacherDomainApprovalService domainApprovalService,
        ITeacherRegistrationCompletionService completionService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _domainApprovalService = domainApprovalService;
        _completionService = completionService;
    }

    public async Task<Response<string>> Handle(
        ApproveTeacherDomainCommand request,
        CancellationToken cancellationToken)
    {
        var (success, error) = await _domainApprovalService.ApproveDomainAsync(
            request.TeacherId,
            request.DomainId,
            request.UserId,
            cancellationToken);

        if (!success)
            return BadRequest<string>(error ?? "Unable to approve domain.");

        await _completionService.RefreshTeacherStatusAfterReviewAsync(request.TeacherId, cancellationToken);

        return Success<string>("Education domain approved for teacher");
    }
}
